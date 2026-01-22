using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Zumlo.Gateway.Application
{
    public class WhisperSpeechToTextEngine : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly List<byte> _audioBuffer = new();

        public WhisperSpeechToTextEngine(string openAiApiKey)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", openAiApiKey);
        }

        /// <summary>
        /// Call on each audio.chunk
        /// </summary>
        public void PushBase64Chunk(string base64Audio)
        {
            var bytes = Convert.FromBase64String(base64Audio);
            _audioBuffer.AddRange(bytes);
        }

        /// <summary>
        /// Call on audio.end
        /// </summary>
        public async Task<string> TranscribeAsync(List<byte> audiobuffer, string language = "en")
        {
            try
            {
                var wavPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");
                await File.WriteAllBytesAsync(wavPath, audiobuffer.ToArray());

                using var form = new MultipartFormDataContent();
                using var audioContent = new ByteArrayContent(await File.ReadAllBytesAsync(wavPath));

                audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

                form.Add(audioContent, "file", "audio.wav");
                form.Add(new StringContent("whisper-1"), "model");
                form.Add(new StringContent(language), "language");

                var response = await _httpClient.PostAsync(
                    "https://api.openai.com/v1/audio/transcriptions",
                    form
                );

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<WhisperResponse>(json);

                File.Delete(wavPath);
                _audioBuffer.Clear();


                return result?.Text ?? string.Empty;
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log them)
                Console.WriteLine($"Error during transcription: {ex.Message}");
                return string.Empty;
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private class WhisperResponse
        {
            public string Text { get; set; } = "";
        }
    }
}
