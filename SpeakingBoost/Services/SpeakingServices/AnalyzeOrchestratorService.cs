using SpeakingBoost.Models.DTOs.Student;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Text.Json.Nodes;

namespace SpeakingBoost.Services.SpeakingServices
{
    public class AnalyzeOrchestratorService
    {
        private readonly SpeechAnalyzeServiceHybrid _speechAnalyze; // Azure STT + Pron
        private readonly EvaluateService _evaluate;                 // OpenAI GPT
        private readonly WebmToWavService _converter;
        private readonly ILogger<AnalyzeOrchestratorService> _logger;

        public AnalyzeOrchestratorService(
            SpeechAnalyzeServiceHybrid speechAnalyze,
            EvaluateService evaluate,
            WebmToWavService converter,
            ILogger<AnalyzeOrchestratorService> logger)
        {
            _speechAnalyze = speechAnalyze;
            _evaluate = evaluate;
            _converter = converter;
            _logger = logger;
        }


        // Trong class AnalyzeOrchestratorService

        // Hàm m?i: X? lý khi file dã n?m trên ? c?ng
        public async Task<AnalyzeResult> ProcessFileAsync(string filePath, string question, int part)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Audio file not found at " + filePath);

            _logger.LogInformation("Orchestrator processing file: {Path}", filePath);

            string? tempWavPath = null;
            string targetWavPath = filePath;

            try
            {
                // STEP 1 — Convert WebM ? WAV (n?u c?n)
                if (!filePath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    // ConvertAsync c?a b?n dã h? tr? input path
                    tempWavPath = await _converter.ConvertAsync(filePath);
                    targetWavPath = tempWavPath;
                }

                // STEP 2 — Duration
                double durationSec = 0;
                try
                {
                    using var reader = new WaveFileReader(targetWavPath);
                    durationSec = reader.TotalTime.TotalSeconds;
                }
                catch { }

                // STEP 3 — Azure Pronunciation (d?c tr?c ti?p t? file path d? gi?m memory spike)
                var pronReportJson = await _speechAnalyze.AnalyzeFromWavPathAsync(
                    wavPath: targetWavPath,
                    referenceText: null,
                    threshold: 80,
                    enableMiscue: true,
                    enableProsody: false,
                    filterFunctionWords: true,
                    filterFillers: true,
                    lang: "en-US",
                    includeRawJson: false
                );

                // ... (Logic còn l?i gi? nguyên nhu hàm ProcessAsync cu) ...
                // L?y transcript
                string transcript = "";
                try
                {
                    var pronNode = JsonNode.Parse(pronReportJson) as JsonObject;
                    transcript = pronNode?["transcript"]?.GetValue<string>() ?? "";
                }
                catch { }

                if (string.IsNullOrWhiteSpace(transcript)) throw new InvalidOperationException("Transcript empty.");

                int wordCount = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

                // GPT Evaluate
                var evalJson = await _evaluate.EvaluateAsync(transcript, question, part, wordCount, durationSec);

                // Merge JSON
                string finalEvalJson = evalJson;
                try
                {
                    var node = JsonNode.Parse(evalJson) as JsonObject;
                    if (node != null)
                    {
                        node["transcript"] = transcript;
                        node["pronunciationReport"] = JsonNode.Parse(pronReportJson);
                        finalEvalJson = node.ToJsonString();
                    }
                }
                catch { }

                return new AnalyzeResult(transcript, finalEvalJson);
            }
            finally
            {
                if (tempWavPath != null) try { File.Delete(tempWavPath); } catch { }
            }
        }


        public async Task<AnalyzeResult> ProcessAsync(IFormFile audio, string question, int part)
        {
            if (audio == null || audio.Length == 0)
                throw new ArgumentException("Audio file is missing.");

            _logger.LogInformation("Orchestrator started | Part={Part} | Size={Size}", part, audio.Length);

            string? tempWavPath = null;
            IFormFile wavFile = audio;

            try
            {
                // STEP 1 — Convert WebM ? WAV (if needed)
                if (!audio.FileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    var tempDir = Path.Combine(Path.GetTempPath(), "IeltsAI_Audio");
                    Directory.CreateDirectory(tempDir);

                    var inputPath = Path.Combine(tempDir, $"{Guid.NewGuid()}_{audio.FileName}");

                    await using (var fs = new FileStream(inputPath, FileMode.Create))
                        await audio.CopyToAsync(fs);

                    tempWavPath = await _converter.ConvertAsync(inputPath);
                    await TryDeleteFileAsync(inputPath);

                    var wavBytes = await File.ReadAllBytesAsync(tempWavPath);
                    var wavStream = new MemoryStream(wavBytes);

                    wavFile = new FormFile(wavStream, 0, wavBytes.Length, "audio", "converted.wav")
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "audio/wav"
                    };
                }

                // STEP 2 — Duration (optional)
                double durationSec = 0;
                try
                {
                    var s = wavFile.OpenReadStream();
                    if (s.CanSeek) s.Position = 0;
                    using var reader = new WaveFileReader(s);
                    durationSec = reader.TotalTime.TotalSeconds;
                }
                catch
                {
                    _logger.LogWarning("?? Could not determine audio duration.");
                }

                // STEP 3 — Azure Pronunciation (t? STT n?u referenceText r?ng)
                // referenceText: b?n có th? truy?n transcript c?a b?n vào dây n?u có,
                // còn không thì d? null/"" d? service t? Azure STT.
                var pronReportJson = await _speechAnalyze.AnalyzeAsync(
                    audioWav: wavFile,
                    referenceText: null,     // ? b? OpenAI TranscriptService => d? Azure STT lo
                    threshold: 80,
                    enableMiscue: true,
                    enableProsody: false,
                    filterFunctionWords: true,
                    filterFillers: true,
                    lang: "en-US",
                    includeRawJson: false
                );

                // L?y transcript t? pronReportJson
                string transcript = "";
                try
                {
                    var pronNode = JsonNode.Parse(pronReportJson) as JsonObject;
                    transcript = pronNode?["transcript"]?.GetValue<string>() ?? "";
                }
                catch { }

                if (string.IsNullOrWhiteSpace(transcript))
                    throw new InvalidOperationException("Transcript is empty (from Azure).");

                // STEP 4 — Word count
                int wordCount = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

                // STEP 5 — Evaluate (OpenAI GPT) dùng transcript Azure
                var evalJson = await _evaluate.EvaluateAsync(
                    transcript: transcript,
                    question: question,
                    part: part,
                    wordCount: wordCount,
                    durationSec: durationSec
                );

                // STEP 6 — Merge JSON: transcript + pronunciationReport
                string finalEvalJson = evalJson;

                try
                {
                    var node = JsonNode.Parse(evalJson) as JsonObject;
                    if (node != null)
                    {
                        node["transcript"] = transcript;
                        node["pronunciationReport"] = JsonNode.Parse(pronReportJson);
                        finalEvalJson = node.ToJsonString();
                    }
                }
                catch { }

                return new AnalyzeResult(transcript, finalEvalJson);
            }
            finally
            {
                if (tempWavPath != null)
                {
                    await TryDeleteFileAsync(tempWavPath);
                }
            }
        }

        private static async Task TryDeleteFileAsync(string? path, int maxAttempts = 3, int delayMs = 120)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    if (!File.Exists(path)) return;
                    File.Delete(path);
                    return;
                }
                catch (IOException)
                {
                    if (i == maxAttempts - 1) return;
                    await Task.Delay(delayMs);
                }
                catch (UnauthorizedAccessException)
                {
                    if (i == maxAttempts - 1) return;
                    await Task.Delay(delayMs);
                }
            }
        }
    }
}
