using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;


namespace Zumlo.Gateway.Application.Audio
{

    public interface IWhisperTranscriber
    {
        Task<WhisperVerboseResult> TranscribeAsync(
            string filePath,
            string model = "whisper-1",
            string? language = null,
            CancellationToken ct = default);
    }

    public record WhisperVerboseResult(string Text, Segment[]? Segments);
    public record Segment(int Id, double Start, double End, string Text);

    public class WhisperTranscriber : IWhisperTranscriber
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public WhisperTranscriber(IHttpClientFactory factory, IConfiguration cfg)
        {
            _http = factory.CreateClient();
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                      ?? cfg["OpenAI:ApiKey"]
                      ?? throw new InvalidOperationException("OPENAI_API_KEY not set");
        }

        public async Task<WhisperVerboseResult> TranscribeAsync(
            string filePath, string model = "whisper-1", string? language = null, CancellationToken ct = default)
        {
            using var form = new MultipartFormDataContent();

            // Required params
            form.Add(new StringContent(model), "model");

            // Ask for detailed segments with timestamps
            form.Add(new StringContent("verbose_json"), "response_format"); // segments[] with start/end [1](https://platform.openai.com/docs/guides/speech-to-text)

            if (!string.IsNullOrWhiteSpace(language))
                form.Add(new StringContent(language), "language");

            await using var fs = File.OpenRead(filePath);
            var fileContent = new StreamContent(fs);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeFromPath(filePath));
            form.Add(fileContent, "file", Path.GetFileName(filePath));

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions")
            {
                Content = form
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            res.EnsureSuccessStatusCode();

            using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            var text = root.GetProperty("text").GetString() ?? string.Empty;

            Segment[]? segments = null;
            if (root.TryGetProperty("segments", out var segs) && segs.ValueKind == JsonValueKind.Array)
            {
                segments = segs.EnumerateArray()
                    .Select(s => new Segment(
                        s.GetProperty("id").GetInt32(),
                        s.GetProperty("start").GetDouble(),
                        s.GetProperty("end").GetDouble(),
                        (s.TryGetProperty("text", out var t) ? t.GetString() : "") ?? string.Empty
                    ))
                    .ToArray();
            }

            return new WhisperVerboseResult(text, segments);
        }

        private static string GetMimeFromPath(string path) =>
            Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".wav" => "audio/wav",
                ".mp3" => "audio/mpeg",
                ".m4a" => "audio/m4a",
                ".webm" => "audio/webm",
                ".mp4" => "audio/mp4",
                ".mpga" => "audio/mpeg",
                _ => "application/octet-stream"
            };
    }

}
