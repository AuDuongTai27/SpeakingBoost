using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpeakingBoost.Services.SpeakingServices
{
    public class TranscriptService
    {
        private readonly HttpClient _client;
        private readonly string _apiKey;
        private readonly string _model;

        public TranscriptService(HttpClient client, IConfiguration config)
        {
            _client = client;

            _apiKey = config["OpenAI:Transcript:ApiKey"]
                ?? throw new InvalidOperationException("OpenAI:Transcript:ApiKey missing");

            _model = config["OpenAI:Transcript:Model"] ?? "whisper-1";
        }

        public async Task<string> TranscribeAsync(IFormFile audio)
        {
            if (audio == null || audio.Length == 0)
                throw new InvalidOperationException("?? Không có file âm thanh ho?c file r?ng.");

            byte[] audioBytes;
            using (var ms = new MemoryStream())
            {
                await audio.CopyToAsync(ms);
                audioBytes = ms.ToArray();
            }

            string fileName = string.IsNullOrWhiteSpace(audio.FileName) ? "audio.wav" : audio.FileName;
            string contentType = string.IsNullOrWhiteSpace(audio.ContentType) ? "application/octet-stream" : audio.ContentType;

            using var form = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(audioBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            form.Add(fileContent, "file", fileName);

            form.Add(new StringContent(_model), "model");
            form.Add(new StringContent("en"), "language");
            form.Add(new StringContent("json"), "response_format");

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey); // ? ch?c ch?n có header
            req.Content = form;

            using var resp = await _client.SendAsync(req);
            string respText = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"? OpenAI STT failed ({(int)resp.StatusCode}): {respText}");

            using var doc = JsonDocument.Parse(respText);
            string finalText = doc.RootElement.GetProperty("text").GetString() ?? "";

            if (string.IsNullOrWhiteSpace(finalText))
                throw new InvalidOperationException("? OpenAI STT không tr? transcript.");

            // cleanup (gi? logic c?a b?n)
            string cleaned = finalText;
            cleaned = new string(cleaned.Where(c => !char.IsControl(c) || c == ' ').ToArray());
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            cleaned = cleaned.Normalize(NormalizationForm.FormC);

            return cleaned;
        }
    }
}
