// File: Services/Background/GradingBackgroundService.cs
using SpeakingBoost.Services.SpeakingServices;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Threading;
using static SpeakingBoost.Models.Entities.Submission;

namespace SpeakingBoost.Services.Background
{
    public class GradingBackgroundService : BackgroundService
    {
        private readonly BackgroundQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GradingBackgroundService> _logger;
        private readonly int _workerCount;
        private readonly int _maxConcurrentJobs;
        private readonly int _maxRetryAttempts;
        private readonly int _jobTimeoutSeconds;
        private readonly SemaphoreSlim _concurrencyGate;
        private long _processedCount;
        private long _successCount;
        private long _failedCount;
        private long _totalLatencyMs;

        public GradingBackgroundService(
            BackgroundQueue queue,
            IServiceProvider serviceProvider,
            ILogger<GradingBackgroundService> logger,
            IConfiguration configuration)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _workerCount = Math.Max(1, configuration.GetValue<int?>("StudentGrading:WorkerCount") ?? 2);
            _maxConcurrentJobs = Math.Max(1, configuration.GetValue<int?>("StudentGrading:MaxConcurrentJobs") ?? _workerCount);
            _maxRetryAttempts = configuration.GetValue<int?>("StudentGrading:MaxRetryAttempts") ?? 2;
            _jobTimeoutSeconds = configuration.GetValue<int?>("StudentGrading:JobTimeoutSeconds") ?? 120;
            _concurrencyGate = new SemaphoreSlim(_maxConcurrentJobs, _maxConcurrentJobs);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "?? Grading Background Service Started with {WorkerCount} workers (max concurrent jobs: {MaxConcurrentJobs}).",
                _workerCount,
                _maxConcurrentJobs);

            var workers = Enumerable.Range(0, _workerCount)
                .Select(workerId => RunWorkerLoopAsync(workerId, stoppingToken))
                .ToArray();

            await Task.WhenAll(workers);
        }

        private async Task RunWorkerLoopAsync(int workerId, CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var submissionId = await _queue.DequeueAsync(stoppingToken);
                    var sw = Stopwatch.StartNew();
                    await _concurrencyGate.WaitAsync(stoppingToken);
                    try
                    {
                        var success = await ProcessSubmissionWithRetryAsync(submissionId, workerId, stoppingToken);
                        sw.Stop();
                        RecordTelemetry(success, sw.ElapsedMilliseconds);
                    }
                    finally
                    {
                        _concurrencyGate.Release();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "? Fatal error in Grading Worker {WorkerId}", workerId);
                }
            }
        }

        private async Task<bool> ProcessSubmissionWithRetryAsync(int submissionId, int workerId, CancellationToken stoppingToken)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= _maxRetryAttempts + 1; attempt++)
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                linkedCts.CancelAfter(TimeSpan.FromSeconds(_jobTimeoutSeconds));

                try
                {
                    await ProcessSubmissionOnceAsync(submissionId, linkedCts.Token);
                    return true;
                }
                catch (OperationCanceledException oce) when (!stoppingToken.IsCancellationRequested)
                {
                    lastException = new TimeoutException($"Grading timed out after {_jobTimeoutSeconds}s", oce);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                var shouldRetry = attempt <= _maxRetryAttempts && IsTransientError(lastException);
                if (!shouldRetry)
                {
                    break;
                }

                if (attempt <= _maxRetryAttempts)
                {
                    var backoffSeconds = Math.Min(8, 1 << (attempt - 1));
                    _logger.LogWarning(lastException,
                        "Retry grading | Worker={WorkerId} | SubmissionId={SubmissionId} | Attempt={Attempt}/{Max} | Delay={Delay}s",
                        workerId, submissionId, attempt, _maxRetryAttempts + 1, backoffSeconds);
                    await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), stoppingToken);
                }
            }

            _logger.LogError(lastException, "? Grading failed after retries | Worker={WorkerId} | SubmissionId={SubmissionId}", workerId, submissionId);
            return false;
        }

        private async Task ProcessSubmissionOnceAsync(int submissionId, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var orchestrator = scope.ServiceProvider.GetRequiredService<AnalyzeOrchestratorService>();
            var submissionHandle = scope.ServiceProvider.GetRequiredService<SubmissionHandleService>();

            var submission = await context.Submissions.FindAsync(new object[] { submissionId }, cancellationToken);
            if (submission == null) return;

            try
            {
                submission.Status = ProcessingStatus.Processing;
                submission.ErrorMessage = null;
                await context.SaveChangesAsync(cancellationToken);

                var exercise = await context.Exercises.FindAsync(new object[] { submission.ExerciseId }, cancellationToken);
                string question = exercise?.Question ?? "Unknown topic";

                int part = 1;
                if (exercise?.Type?.ToLower().Contains("part2") == true) part = 2;
                if (exercise?.Type?.ToLower().Contains("part3") == true) part = 3;

                string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                if (string.IsNullOrWhiteSpace(submission.AudioPath))
                    throw new InvalidOperationException("AudioPath is empty.");

                string relativePath = submission.AudioPath.TrimStart('/');
                string fullPath = Path.Combine(webRootPath, relativePath);

                var result = await orchestrator
                    .ProcessFileAsync(fullPath, question, part)
                    .WaitAsync(cancellationToken);

                await submissionHandle.UpdateResultAsync(submissionId, result.Transcript, result.EvalJson);

                submission.Status = ProcessingStatus.Completed;
                submission.ErrorMessage = null;
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("? Ch?m xong bài SubmissionId={SubmissionId}", submissionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? L?i ch?m bài SubmissionId={SubmissionId}", submissionId);
                submission.Status = ProcessingStatus.Failed;
                submission.ErrorMessage = ex.Message;
                await context.SaveChangesAsync(cancellationToken);
                throw;
            }
        }

        private static bool IsTransientError(Exception? ex)
        {
            if (ex == null) return false;
            if (ex is TimeoutException || ex is HttpRequestException || ex is TaskCanceledException) return true;
            if (ex is InvalidOperationException ioe)
            {
                var msg = ioe.Message ?? string.Empty;
                if (msg.Contains("OpenAI request failed", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("JSON", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("timed out", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void RecordTelemetry(bool success, long latencyMs)
        {
            var processed = Interlocked.Increment(ref _processedCount);
            if (success) Interlocked.Increment(ref _successCount);
            else Interlocked.Increment(ref _failedCount);

            Interlocked.Add(ref _totalLatencyMs, latencyMs);

            if (processed % 10 != 0) return;

            var ok = Interlocked.Read(ref _successCount);
            var fail = Interlocked.Read(ref _failedCount);
            var totalLatency = Interlocked.Read(ref _totalLatencyMs);
            var avgLatency = processed > 0 ? totalLatency / processed : 0;
            var failRate = processed > 0 ? (double)fail / processed * 100 : 0;

            _logger.LogInformation(
                "?? Grading telemetry | Processed={Processed} | Success={Success} | Failed={Failed} | FailRate={FailRate:F1}% | AvgLatencyMs={AvgLatencyMs}",
                processed, ok, fail, failRate, avgLatency);
        }
    }
}
