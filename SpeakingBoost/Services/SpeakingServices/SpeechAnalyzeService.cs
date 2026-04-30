using Microsoft.AspNetCore.Http;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SpeakingBoost.Services.SpeakingServices
{
    public class SpeechAnalyzeServiceHybrid
    {
        private readonly string _speechKey;
        private readonly string _region;

        private const string DefaultLang = "en-US";
        private const double TicksPerSecond = 10_000_000.0; // 100ns ticks

        // Function words (b? sung d?n n?u c?n)
        private static readonly HashSet<string> FunctionWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the","a","an","and","or","but","so",
            "to","of","in","on","at","for","with","from","as",
            "is","are","was","were","be","been","being",
            "do","does","did","have","has","had",
            "will","would","can","could","should","may","might","must",
            "that","this","these","those","it",
            "i","you","he","she","we","they","me","him","her","us","them",
            "my","your","his","her","our","their",
            "not","no","yes","then","than"
        };

        // filler / disfluency
        private static readonly HashSet<string> Fillers = new(StringComparer.OrdinalIgnoreCase)
        {
            "um","uh","erm","er","ah","hmm","mm","mmm","uhh","umm"
        };

        public SpeechAnalyzeServiceHybrid(IConfiguration config)
        {
            _speechKey = config["AzureSpeech:ApiKey"]
                ?? throw new InvalidOperationException("AzureSpeech:ApiKey missing");

            _region = config["AzureSpeech:Region"]
                ?? throw new InvalidOperationException("AzureSpeech:Region missing");
        }

        /// <summary>
        /// Analyze pronunciation and return report JSON (filtered low words + phoneme details).
        /// referenceText nên là transcript (OpenAI STT) ho?c script expected. N?u r?ng -> s? Azure STT r?i dùng transcript làm reference (t?n thêm 1 call).
        /// </summary>
        public async Task<string> AnalyzeAsync(
            IFormFile audioWav,
            string? referenceText,
            double threshold = 80.0,
            bool enableMiscue = true,
            bool enableProsody = false,
            bool filterFunctionWords = true,
            bool filterFillers = true,
            string lang = DefaultLang,
            bool includeRawJson = false)
        {
            if (audioWav == null || audioWav.Length == 0)
                throw new InvalidOperationException("? File audio r?ng.");

            // Save to temp wav (?n d?nh nh?t cho Speech SDK)
            var tempDir = Path.Combine(Path.GetTempPath(), "IeltsAI_Pron");
            Directory.CreateDirectory(tempDir);

            var wavPath = Path.Combine(tempDir, $"{Guid.NewGuid()}_{Path.GetFileName(audioWav.FileName)}");
            await using (var fs = new FileStream(wavPath, FileMode.Create, FileAccess.Write))
            {
                await audioWav.CopyToAsync(fs);
            }

            try
            {
                // N?u reference r?ng -> Azure STT (gi?ng pipeline m?i nh?t) (t?n thêm)
                var refText = (referenceText ?? "").Trim();
                if (string.IsNullOrWhiteSpace(refText))
                {
                    refText = await AzureSttAsync(wavPath, lang);
                    if (string.IsNullOrWhiteSpace(refText))
                        throw new InvalidOperationException("STT returned empty transcript. Provide referenceText (prefer OpenAI transcript).");
                }

                var (rawJson, overall, transcript) = await AzurePronunciationAsync(
                    wavPath: wavPath,
                    referenceText: refText,
                    lang: lang,
                    enableMiscue: enableMiscue,
                    enableProsody: enableProsody
                );

                var report = BuildReport(
                    rawJson: rawJson,
                    transcript: transcript,
                    reference: refText,
                    overall: overall,
                    threshold: threshold,
                    filterFunctionWords: filterFunctionWords,
                    filterFillers: filterFillers,
                    includeRawJson: includeRawJson
                );

                return JsonSerializer.Serialize(report, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            finally
            {
                await TryDeleteFileAsync(wavPath);
            }
        }

        public async Task<string> AnalyzeFromWavPathAsync(
            string wavPath,
            string? referenceText,
            double threshold = 80.0,
            bool enableMiscue = true,
            bool enableProsody = false,
            bool filterFunctionWords = true,
            bool filterFillers = true,
            string lang = DefaultLang,
            bool includeRawJson = false)
        {
            if (string.IsNullOrWhiteSpace(wavPath) || !File.Exists(wavPath))
                throw new FileNotFoundException($"? WAV file not found: {wavPath}");

            var refText = (referenceText ?? "").Trim();
            if (string.IsNullOrWhiteSpace(refText))
            {
                refText = await AzureSttAsync(wavPath, lang);
                if (string.IsNullOrWhiteSpace(refText))
                    throw new InvalidOperationException("STT returned empty transcript. Provide referenceText (prefer OpenAI transcript).");
            }

            var (rawJson, overall, transcript) = await AzurePronunciationAsync(
                wavPath: wavPath,
                referenceText: refText,
                lang: lang,
                enableMiscue: enableMiscue,
                enableProsody: enableProsody
            );

            var report = BuildReport(
                rawJson: rawJson,
                transcript: transcript,
                reference: refText,
                overall: overall,
                threshold: threshold,
                filterFunctionWords: filterFunctionWords,
                filterFillers: filterFillers,
                includeRawJson: includeRawJson
            );

            return JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        // ================= Azure calls =================

        private async Task<string> AzureSttAsync(string wavPath, string lang)
        {
            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _region);
            speechConfig.SpeechRecognitionLanguage = lang;
            speechConfig.OutputFormat = OutputFormat.Detailed;

            using var audioConfig = AudioConfig.FromWavFileInput(wavPath);
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            var segments = await RunContinuousAsync(recognizer);


            if (segments.Count == 0)
                throw new InvalidOperationException("Azure STT: NoMatch (no segments recognized).");

            var sb = new StringBuilder();
            foreach (var seg in segments)
            {
                var display = TryGetNBestDisplay(seg.rawJson) ?? seg.text;
                if (string.IsNullOrWhiteSpace(display)) continue;

                if (sb.Length > 0) sb.Append(' ');
                sb.Append(display.Trim());
            }

            return sb.ToString().Trim();
        }

        private async Task<(string rawJson, OverallScores overall, string transcript)> AzurePronunciationAsync(
            string wavPath,
            string referenceText,
            string lang,
            bool enableMiscue,
            bool enableProsody)
        {
            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _region);
            speechConfig.SpeechRecognitionLanguage = lang;
            speechConfig.OutputFormat = OutputFormat.Detailed;

            using var audioConfig = AudioConfig.FromWavFileInput(wavPath);
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            var paCfg = new PronunciationAssessmentConfig(
                referenceText,
                GradingSystem.HundredMark,
                Granularity.Phoneme,
                enableMiscue: enableMiscue
            );

            // IPA phoneme output
            try { paCfg.PhonemeAlphabet = "IPA"; } catch { }

            if (enableProsody)
                paCfg.EnableProsodyAssessment();

            paCfg.ApplyTo(recognizer);

            var segments = await RunContinuousAsync(recognizer);

            if (segments.Count == 0)
                throw new InvalidOperationException("Azure Speech: NoMatch (check wav format / referenceText).");

            // transcript: uu tiên NBest[0].Display
            var transcriptSb = new StringBuilder();
            foreach (var seg in segments)
            {
                var display = TryGetNBestDisplay(seg.rawJson) ?? seg.text;
                if (string.IsNullOrWhiteSpace(display)) continue;

                if (transcriptSb.Length > 0) transcriptSb.Append(' ');
                transcriptSb.Append(display.Trim());
            }

            var transcript = transcriptSb.ToString().Trim();

            // rawJson: merge nhi?u segment -> 1 JSON có NBest[0].Words[] d? BuildReport dùng nhu cu
            var rawList = new List<string>(segments.Count);
            foreach (var s in segments) rawList.Add(s.rawJson);

            var rawJson = MergeSpeechJsonResults(rawList);

            if (string.IsNullOrWhiteSpace(rawJson))
                throw new InvalidOperationException("Azure returned empty merged JsonResult.");

            // overall: ? mode B (weight theo s? lu?ng Words m?i segment)
            var overall = AggregateOverallScoresByWordCount(segments);

            return (rawJson, overall, transcript);
        }

        // ================= Continuous helpers =================

        private sealed record RecognizedSegment(string text, string rawJson, PronunciationAssessmentResult? paResult);

        private async Task<List<RecognizedSegment>> RunContinuousAsync(SpeechRecognizer recognizer)
        {
            var segments = new List<RecognizedSegment>();
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            recognizer.Recognized += (_, e) =>
            {
                if (e.Result == null) return;

                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    var rawJson = e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult) ?? "";
                    var text = (e.Result.Text ?? "").Trim();

                    PronunciationAssessmentResult? pa = null;
                    try { pa = PronunciationAssessmentResult.FromResult(e.Result); } catch { /* STT mode won't have PA */ }

                    if (!string.IsNullOrWhiteSpace(rawJson) || !string.IsNullOrWhiteSpace(text))
                        segments.Add(new RecognizedSegment(text, rawJson, pa));
                }
                // NoMatch: b? qua (d?ng throw), vì có th? các segment khác v?n OK
            };

            recognizer.Canceled += (_, e) =>
            {
                // V?i input file (FromWavFileInput), EndOfStream thu?ng ch? có nghia là dã d?c h?t audio,
                // không ph?i l?i. Ch? throw khi th?c s? là Error.
                if (e.Reason != CancellationReason.Error)
                {
                    tcs.TrySetResult(true);
                    return;
                }

                var msg = $"Azure canceled: {e.Reason} | {e.ErrorDetails}";
                tcs.TrySetException(new InvalidOperationException(msg));
            };

            recognizer.SessionStopped += (_, __) =>
            {
                tcs.TrySetResult(true);
            };

            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
            try
            {
                await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                try { await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false); }
                catch { /* ignore */ }
            }

            return segments;
        }

        private static string? TryGetNBestDisplay(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return null;
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var nbest = doc.RootElement.GetProperty("NBest")[0];
                if (nbest.TryGetProperty("Display", out var disp))
                {
                    var d = (disp.GetString() ?? "").Trim();
                    return string.IsNullOrWhiteSpace(d) ? null : d;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Merge nhi?u SpeechServiceResponse_JsonResult thành 1 JSON t?i thi?u:
        /// { "NBest":[ { "Display":"...", "Words":[ ...merged... ] } ] }
        /// N?u offset trong segment b? reset, hàm s? c? g?ng shift d? timeline tang d?n.
        /// </summary>
        private static string MergeSpeechJsonResults(IReadOnlyList<string> rawJsons)
        {
            var mergedWords = new JsonArray();
            var mergedDisplay = new StringBuilder();

            long lastEnd = 0; // ticks

            foreach (var raw in rawJsons)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;

                JsonObject? root;
                try { root = JsonNode.Parse(raw) as JsonObject; }
                catch { continue; }

                var nbest0 = root?["NBest"]?[0] as JsonObject;
                if (nbest0 == null) continue;

                var disp = (nbest0["Display"]?.GetValue<string>() ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(disp))
                {
                    if (mergedDisplay.Length > 0) mergedDisplay.Append(' ');
                    mergedDisplay.Append(disp);
                }

                var words = nbest0["Words"] as JsonArray;
                if (words == null || words.Count == 0) continue;

                // detect shift (n?u offset b? reset trong continuous)
                long firstOff = GetLong(words[0]?["Offset"]);
                long shift = 0;
                if (firstOff > 0 && firstOff < lastEnd)
                    shift = lastEnd - firstOff;

                foreach (var wNode in words)
                {
                    if (wNode is not JsonObject wObj) continue;

                    if (shift != 0) ShiftOffsetsRecursively(wObj, shift);

                    var cloned = wObj.DeepClone();
                    mergedWords.Add(cloned);

                    long off = GetLong(cloned?["Offset"]);
                    long dur = GetLong(cloned?["Duration"]);
                    lastEnd = Math.Max(lastEnd, off + dur);
                }
            }

            var mergedNBest0 = new JsonObject
            {
                ["Display"] = mergedDisplay.ToString(),
                ["Words"] = mergedWords
            };

            var mergedRoot = new JsonObject
            {
                ["NBest"] = new JsonArray(mergedNBest0)
            };

            return mergedRoot.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        private static long GetLong(JsonNode? node)
        {
            try
            {
                if (node == null) return 0;
                return node.GetValue<long>();
            }
            catch { return 0; }
        }

        private static void ShiftOffsetsRecursively(JsonNode node, long shift)
        {
            if (shift == 0 || node == null) return;

            if (node is JsonObject obj)
            {
                if (obj.TryGetPropertyValue("Offset", out var offNode) && offNode != null)
                {
                    if (long.TryParse(offNode.ToString(), out var off) && off > 0)
                        obj["Offset"] = off + shift;
                }

                // copy to avoid modifying while iterating
                var kvs = new List<KeyValuePair<string, JsonNode?>>();
                foreach (var kv in obj) kvs.Add(kv);

                foreach (var kv in kvs)
                    if (kv.Value != null) ShiftOffsetsRecursively(kv.Value, shift);
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr)
                    if (item != null) ShiftOffsetsRecursively(item, shift);
            }
        }

        private static OverallScores AggregateOverallScoresByWordCount(IReadOnlyList<RecognizedSegment> segments)
        {
            double sumW = 0;

            double p = 0, a = 0, f = 0, pr = 0, c = 0;

            foreach (var seg in segments)
            {
                if (seg.paResult == null) continue;

                var w = Math.Max(1, CountWordsInRawJson(seg.rawJson));

                sumW += w;

                p += seg.paResult.PronunciationScore * w;
                a += seg.paResult.AccuracyScore * w;
                f += seg.paResult.FluencyScore * w;
                pr += seg.paResult.ProsodyScore * w;
                c += seg.paResult.CompletenessScore * w;
            }

            if (sumW <= 0) return new OverallScores();

            return new OverallScores
            {
                pronunciationScore = p / sumW,
                accuracy = a / sumW,
                fluency = f / sumW,
                prosody = pr / sumW,
                completeness = c / sumW
            };
        }

        private static int CountWordsInRawJson(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return 0;
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var nbest = doc.RootElement.GetProperty("NBest")[0];
                if (nbest.TryGetProperty("Words", out var arr) && arr.ValueKind == JsonValueKind.Array)
                    return arr.GetArrayLength();
            }
            catch { }
            return 0;
        }

        // ================= Report building =================

        // Trong file SpeechAnalyzeService.cs

        private static Report BuildReport(
            string rawJson,
            string transcript,
            string reference,
            OverallScores overall,
            double threshold,
            bool filterFunctionWords,
            bool filterFillers,
            bool includeRawJson)
        {
            using var doc = JsonDocument.Parse(rawJson);
            var nbest = doc.RootElement.GetProperty("NBest")[0];

            var lowWords = new List<LowWord>();

            // ?? S?A 1: Bi?n d?m v? trí th?c t? c?a t? (Index), b?t d?u t? 0
            int currentWordIndex = 0;

            if (nbest.TryGetProperty("Words", out var wordArr) && wordArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var w in wordArr.EnumerateArray())
                {
                    // ?? S?A 2: L?y Index hi?n t?i làm ID, sau dó tang lên ngay l?p t?c
                    // Làm v?y d? d?m b?o Index kh?p 1-1 v?i m?ng words bên Frontend
                    int thisId = currentWordIndex;
                    currentWordIndex++;

                    var wordText = w.TryGetProperty("Word", out var we) ? (we.GetString() ?? "") : "";
                    if (string.IsNullOrWhiteSpace(wordText)) continue;

                    var norm = NormalizeToken(wordText);

                    // Các b? l?c (n?u l?c b?, ta v?n dã tang currentWordIndex ? trên r?i -> Index v?n dúng)
                    if (filterFillers && Fillers.Contains(norm)) continue;
                    if (filterFunctionWords && FunctionWords.Contains(norm)) continue;

                    if (!w.TryGetProperty("PronunciationAssessment", out var paWord)) continue;

                    if (!paWord.TryGetProperty("AccuracyScore", out var accEl) || accEl.ValueKind != JsonValueKind.Number)
                        continue;

                    var acc = accEl.GetDouble();

                    // N?u di?m cao hon ngu?ng -> B? qua
                    if (acc >= threshold) continue;

                    // --- L?y d? li?u chi ti?t ---
                    string? errType = paWord.TryGetProperty("ErrorType", out var etEl) ? etEl.GetString() : null;

                    long offset = w.TryGetProperty("Offset", out var offEl) && offEl.ValueKind == JsonValueKind.Number
                        ? offEl.GetInt64() : 0;

                    long duration = w.TryGetProperty("Duration", out var durEl) && durEl.ValueKind == JsonValueKind.Number
                        ? durEl.GetInt64() : 0;

                    var startSec = Math.Round(offset / TicksPerSecond, 3);
                    var durSec = Math.Round(duration / TicksPerSecond, 3);
                    var endSec = Math.Round(startSec + durSec, 3);

                    var phonemes = ExtractPhonemesFromWord(w);

                    lowWords.Add(new LowWord
                    {
                        // ?? S?A 3: Gán ID b?ng v? trí th?c t?
                        id = thisId,

                        word = wordText,
                        normalized = norm,
                        accuracyScore = acc,
                        errorType = errType,
                        offset = offset,
                        duration = duration,
                        startSec = startSec,
                        durationSec = durSec,
                        endSec = endSec,
                        phonemes = phonemes
                    });
                }
            }

            return new Report
            {
                transcript = transcript,
                reference = reference,
                threshold = threshold,
                overall = overall,
                low_words = lowWords,
                rawJson = includeRawJson ? rawJson : null
            };
        }

        private static List<PhonemeDetail> ExtractPhonemesFromWord(JsonElement word)
        {
            var list = new List<PhonemeDetail>();

            // ? uu tiên Words[].Phonemes[]
            if (word.TryGetProperty("Phonemes", out var phArr) && phArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in phArr.EnumerateArray())
                    AddPh(p, list);
                return list;
            }

            // fallback: Words[].Syllables[].Phonemes[]
            if (word.TryGetProperty("Syllables", out var syllArr) && syllArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in syllArr.EnumerateArray())
                {
                    if (!s.TryGetProperty("Phonemes", out var ph2) || ph2.ValueKind != JsonValueKind.Array) continue;
                    foreach (var p in ph2.EnumerateArray())
                        AddPh(p, list);
                }
            }

            return list;

            static void AddPh(JsonElement p, List<PhonemeDetail> list)
            {
                var ph = p.TryGetProperty("Phoneme", out var phEl) ? (phEl.GetString() ?? "") : "";
                if (string.IsNullOrWhiteSpace(ph)) return;

                double? acc = null;
                string? errType = null;

                if (p.TryGetProperty("PronunciationAssessment", out var pa))
                {
                    if (pa.TryGetProperty("AccuracyScore", out var a) && a.ValueKind == JsonValueKind.Number)
                        acc = a.GetDouble();
                    if (pa.TryGetProperty("ErrorType", out var e))
                        errType = e.GetString();
                }

                long offset = p.TryGetProperty("Offset", out var offEl) && offEl.ValueKind == JsonValueKind.Number ? offEl.GetInt64() : 0;
                long duration = p.TryGetProperty("Duration", out var durEl) && durEl.ValueKind == JsonValueKind.Number ? durEl.GetInt64() : 0;

                double? startSec = offset == 0 ? null : Math.Round(offset / TicksPerSecond, 3);
                double? durSec = duration == 0 ? null : Math.Round(duration / TicksPerSecond, 3);
                double? endSec = (startSec.HasValue && durSec.HasValue) ? Math.Round(startSec.Value + durSec.Value, 3) : null;

                list.Add(new PhonemeDetail
                {
                    phoneme = ph,
                    accuracyScore = acc,
                    errorType = errType,
                    offset = offset,
                    duration = duration,
                    startSec = startSec,
                    durationSec = durSec,
                    endSec = endSec
                });
            }
        }

        private static string NormalizeToken(string w)
        {
            if (string.IsNullOrWhiteSpace(w)) return "";
            w = w.Trim();
            // strip punctuation d?u/cu?i, gi? ch?/s?/' ? gi?a (don't, I'm)
            w = Regex.Replace(w, @"^[^\p{L}\p{N}']+|[^\p{L}\p{N}']+$", "");
            return w.ToLowerInvariant();
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

        // ================= DTOs =================

        private sealed class Report
        {
            public string transcript { get; set; } = "";
            public string reference { get; set; } = "";
            public double threshold { get; set; }
            public OverallScores overall { get; set; } = new();
            public List<LowWord> low_words { get; set; } = new();
            public string? rawJson { get; set; } // null n?u includeRawJson=false
        }

        private sealed class OverallScores
        {
            public double pronunciationScore { get; set; }
            public double accuracy { get; set; }
            public double fluency { get; set; }
            public double prosody { get; set; }
            public double completeness { get; set; }
        }

        private sealed class LowWord
        {
            public int id { get; set; }
            public string word { get; set; } = "";
            public string normalized { get; set; } = "";
            public double accuracyScore { get; set; }
            public string? errorType { get; set; }
            public long offset { get; set; }
            public long duration { get; set; }
            public double startSec { get; set; }
            public double durationSec { get; set; }
            public double endSec { get; set; }
            public List<PhonemeDetail> phonemes { get; set; } = new();

        }

        private sealed class PhonemeDetail
        {
            public string phoneme { get; set; } = "";
            public double? accuracyScore { get; set; }
            public string? errorType { get; set; }
            public long offset { get; set; }
            public long duration { get; set; }
            public double? startSec { get; set; }
            public double? durationSec { get; set; }
            public double? endSec { get; set; }
        }
    }
}
