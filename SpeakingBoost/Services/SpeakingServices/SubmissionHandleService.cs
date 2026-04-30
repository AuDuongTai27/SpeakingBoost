using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SpeakingBoost.Services.SpeakingServices
{
    public class SubmissionHandleService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubmissionHandleService> _logger;

        public SubmissionHandleService(ApplicationDbContext context, ILogger<SubmissionHandleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// T?o Submission + Score (t? JSON tr? v? b?i EvaluateService).
        /// aiJson: JSON c?a GPT, có th? dã merge thêm pronunciationReport t? Azure.
        /// pronunciation: n?u b?n dã có band Pronunciation (0-9) thì truy?n vào d? override.
        /// </summary>
        public async Task<(Submission submission, Score score)> ProcessAsync(
            int studentId,
            int exerciseId,
            string transcript,
            string audioPath,
            string aiJson,
            double? pronunciation = null
        )
        {
            _logger.LogInformation("?? [SubmissionHandle] Creating submission for student={Student}, exercise={Exercise}",
                studentId, exerciseId);

            // ----------------------
            // 1) Parse JSON l?y di?m IELTS (3 criteria t? GPT)
            // ----------------------
            double fluencyCoherence;
            double lexicalResource;
            double grammar;

            // pronunciationBand: IELTS-style (0-9, step 0.5)
            double? pronunciationBand = pronunciation;

            try
            {
                using var doc = JsonDocument.Parse(aiJson);
                var root = doc.RootElement;

                fluencyCoherence = GetScore(root, "fluency_coherence");
                lexicalResource = GetScore(root, "lexical_resource");
                grammar = GetScore(root, "grammar");

                // ----------------------
                // 1.1) N?u có pronunciationReport (Azure):
                // - (A) N?u chua có pronunciationBand: l?y pronunciationScore (0-100) -> IELTS band
                // - (B) Fluency&Coherence: l?y Azure fluency (0-100) -> IELTS band, r?i l?y MIN v?i GPT
                // ----------------------
                if (root.TryGetProperty("pronunciationReport", out var pronReport))
                {
                    // ---- (A) Pronunciation band (n?u chua có) ----
                    if (!pronunciationBand.HasValue)
                    {
                        // pronReport.overall.pronunciationScore (0-100)
                        if (pronReport.TryGetProperty("overall", out var pronOverall) &&
                            pronOverall.TryGetProperty("pronunciationScore", out var ps) &&
                            ps.ValueKind == JsonValueKind.Number)
                        {
                            var pronScore100 = ps.GetDouble();
                            pronunciationBand = MapAzure100ToIeltsBand(pronScore100);
                        }

                        // N?u JSON c?a b?n dùng "pronScore" thay vì "pronunciationScore"
                        if (!pronunciationBand.HasValue &&
                            pronReport.TryGetProperty("overall", out var overall2) &&
                            overall2.TryGetProperty("pronScore", out var ps2) &&
                            ps2.ValueKind == JsonValueKind.Number)
                        {
                            var pronScore100 = ps2.GetDouble();
                            pronunciationBand = MapAzure100ToIeltsBand(pronScore100);
                        }
                    }

                    // ---- (B) Fluency cap cho Fluency&Coherence ----
                    // JSON b?n dua: pronunciationReport.overall.fluency (0-100)
                    if (pronReport.TryGetProperty("overall", out var overallF))
                    {
                        JsonElement fs;
                        bool hasFluency =
                            overallF.TryGetProperty("fluency", out fs) ||        // ? theo schema b?n g?i
                            overallF.TryGetProperty("fluencyScore", out fs);     // fallback

                        if (hasFluency && fs.ValueKind == JsonValueKind.Number)
                        {
                            var fluencyScore100 = fs.GetDouble();
                            var azureFluencyBand = MapAzure100ToIeltsBand(fluencyScore100);

                            // l?y di?m th?p nh?t c?a 2 ngu?n + làm tròn xu?ng 0.5
                            fluencyCoherence = RoundDownToHalf(Math.Min(fluencyCoherence, azureFluencyBand));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"? Failed to parse AI JSON score structure: {ex.Message}\nJSON: {aiJson}"
                );
            }

            // ----------------------
            // 2) T?o Submission
            // ----------------------
            var submission = new Submission
            {
                StudentId = studentId,
                ExerciseId = exerciseId,
                Transcript = transcript,
                AudioPath = audioPath,
                CreatedAt = DateTime.Now
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("?? [SubmissionHandle] Submission created (ID={Id})", submission.SubmissionId);

            // ----------------------
            // 3) Tính Overall
            // - Có pronunciationBand => avg 4 criteria
            // - Không có => avg 3 criteria
            // IELTS rule: round down to nearest 0.5
            // ----------------------
            double overall;
            if (pronunciationBand.HasValue)
            {
                double avg4 = (fluencyCoherence + lexicalResource + grammar + pronunciationBand.Value) / 4.0;
                overall = RoundDownToHalf(avg4);
            }
            else
            {
                double avg3 = (fluencyCoherence + lexicalResource + grammar) / 3.0;
                overall = RoundDownToHalf(avg3);
            }

            // ----------------------
            // 4) T?o Score g?n vào Submission
            // ----------------------
            var score = new Score
            {
                SubmissionId = submission.SubmissionId,

                // Pronunciation band (0-9, step 0.5) n?u có
                Pronunciation = pronunciationBand,

                // Map JSON ? DB Columns
                Grammar = grammar,
                LexicalResource = lexicalResource,
                Coherence = fluencyCoherence,
                Overall = overall,
                AiFeedback = aiJson,
                CreatedAt = DateTime.Now
            };

            typeof(Score).GetProperty("Overall")?.SetValue(score, overall);

            _context.Scores.Add(score);
            await _context.SaveChangesAsync();

            _logger.LogInformation("?? [SubmissionHandle] Score saved (ScoreId={Id}, Overall={Score}, Pron={Pron})",
                score.ScoreId, overall, pronunciationBand?.ToString() ?? "N/A");

            return (submission, score);
        }

        // ===== Helpers =====

        private static double GetScore(JsonElement root, string criteriaKey)
        {
            if (!root.TryGetProperty(criteriaKey, out var obj))
                throw new InvalidOperationException($"Missing '{criteriaKey}' in AI JSON.");

            if (!obj.TryGetProperty("score", out var scoreEl) || scoreEl.ValueKind != JsonValueKind.Number)
                throw new InvalidOperationException($"Missing '{criteriaKey}.score' in AI JSON.");

            return scoreEl.GetDouble();
        }

        private static double RoundDownToHalf(double x)
            => Math.Floor(x * 2.0) / 2.0;

        /// <summary>
        /// Estimate mapping: Azure score (0-100) -> IELTS band (0-9),
        /// then round down to 0.5.
        /// Không ph?i công th?c IELTS chính th?c; luu d? hi?n th?/so sánh,
        /// v?n gi? raw JSON d? sau này calibrate.
        /// </summary>
        private static double MapAzure100ToIeltsBand(double pronScore100)
        {
            // clamp
            if (pronScore100 < 0) pronScore100 = 0;
            if (pronScore100 > 100) pronScore100 = 100;
            pronScore100 = Math.Clamp(pronScore100, 0, 100);

            double band =
                4 + 4.5 / (1.0 + Math.Exp(-0.08 * (pronScore100 - 75)));

            if (band < 0) band = 0;
            if (band > 9) band = 9;

            return RoundDownToHalf(band);
        }

        // Trong class SubmissionHandleService

        public async Task UpdateResultAsync(int submissionId, string transcript, string aiJson)
        {
            var submission = await _context.Submissions.FindAsync(submissionId);
            if (submission == null) throw new Exception("Submission not found");

            submission.Transcript = transcript;

            // 1. Parse di?m (Logic copy t? ProcessAsync cu)
            double fluencyCoherence = 0, lexicalResource = 0, grammar = 0;
            double? pronunciationBand = null;

            using var doc = JsonDocument.Parse(aiJson);
            var root = doc.RootElement;

            // L?y 3 di?m co b?n t? GPT
            fluencyCoherence = GetScore(root, "fluency_coherence");
            lexicalResource = GetScore(root, "lexical_resource");
            grammar = GetScore(root, "grammar");

            // 2. Logic Pronunciation mapping (ÐÃ S?A: Thêm code vào block này)
            if (root.TryGetProperty("pronunciationReport", out var pronReport))
            {
                // ---- (A) L?y di?m Pronunciation (Azure 0-100 -> IELTS 0-9) ----
                if (pronReport.TryGetProperty("overall", out var pronOverall))
                {
                    // Th? l?y "pronunciationScore"
                    if (pronOverall.TryGetProperty("pronunciationScore", out var ps) && ps.ValueKind == JsonValueKind.Number)
                    {
                        var pronScore100 = ps.GetDouble();
                        pronunciationBand = MapAzure100ToIeltsBand(pronScore100);
                    }
                    // Fallback: Th? l?y "pronScore" (n?u tên bi?n khác)
                    else if (pronOverall.TryGetProperty("pronScore", out var ps2) && ps2.ValueKind == JsonValueKind.Number)
                    {
                        var pronScore100 = ps2.GetDouble();
                        pronunciationBand = MapAzure100ToIeltsBand(pronScore100);
                    }

                    // ---- (B) Logic "Cap" di?m Fluency d?a trên Azure (Optional) ----
                    JsonElement fs;
                    bool hasFluency = pronOverall.TryGetProperty("fluency", out fs) || pronOverall.TryGetProperty("fluencyScore", out fs);

                    if (hasFluency && fs.ValueKind == JsonValueKind.Number)
                    {
                        var fluencyScore100 = fs.GetDouble();
                        var azureFluencyBand = MapAzure100ToIeltsBand(fluencyScore100);
                        // L?y min gi?a GPT và Azure d? tránh GPT ch?m quá tay
                        fluencyCoherence = RoundDownToHalf(Math.Min(fluencyCoherence, azureFluencyBand));
                    }
                }
            }

            // 3. Tính Overall
            double overall;
            if (pronunciationBand.HasValue)
                overall = RoundDownToHalf((fluencyCoherence + lexicalResource + grammar + pronunciationBand.Value) / 4.0);
            else
                overall = RoundDownToHalf((fluencyCoherence + lexicalResource + grammar) / 3.0);

            // 4. T?o Score m?i liên k?t v?i Submission cu
            var score = new Score
            {
                SubmissionId = submissionId,
                Grammar = grammar,
                LexicalResource = lexicalResource,
                Coherence = fluencyCoherence,
                Pronunciation = pronunciationBand,
                Overall = overall,
                AiFeedback = aiJson,
                CreatedAt = DateTime.Now
            };

            // Fix l?i Overall không set du?c n?u là property tính toán (nhung ? dây b?n dang gán dè nên ok)
            // N?u Score.Overall có setter private/protected thì dùng Reflection nhu cu, còn public set thì gán th?ng:
            typeof(Score).GetProperty("Overall")?.SetValue(score, overall);

            _context.Scores.Add(score);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"? Updated Result for Submission {submissionId}. Pronunciation Band: {pronunciationBand}");
        }
    }
}
