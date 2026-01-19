
using System.Text;
using Zumlo.Gateway.Domain;

using Microsoft.Extensions.Logging;
namespace Zumlo.Gateway.Application.Audio
{
    public class AudioNormalizer
    {
        // Placeholder for normalization, e.g., resample to 16k mono PCM
        public byte[] Normalize(byte[] raw) => raw;
    }

    public class TranscriptionSimulator
    {
        private readonly ILogger<TranscriptionSimulator> _logger;
        public TranscriptionSimulator(ILogger<TranscriptionSimulator> logger) => _logger = logger;

        public async IAsyncEnumerable<TranscriptSegment> StreamTranscriptAsync(string sessionId, SessionManager manager)
        {
            var session = manager.GetOrThrow(sessionId);
            var byteLen = session.AudioBuffer.Length;
            // Simulate words proportional to audio size
            var words = new List<string>{"this","is","a","simulated","transcript","for","your","voice","session"};
            var repeat = Math.Clamp(byteLen / 4000, 1, 30);
            var streamWords = Enumerable.Repeat(words, repeat).SelectMany(x => x).Take(40).ToArray();

            double t = 0;
            for (int i = 0; i < streamWords.Length; i++)
            {
                var w = streamWords[i];
                var seg = new TranscriptSegment { Text = w, StartMs = t, EndMs = t + 250 };
                manager.AddTranscriptSegment(sessionId, seg);
                t += 250;
                await Task.Delay(120);
                yield return seg;
            }
        }
    }

    public class AssistantResponseSimulator
    {
        public async IAsyncEnumerable<string> StreamResponseAsync(string sessionId, SessionManager manager)
        {
            manager.MarkResponding(sessionId);
            var s = manager.GetOrThrow(sessionId);
            var full = string.Join(' ', s.Segments.Select(x => x.Text));

            var response = string.IsNullOrWhiteSpace(full)
                ? "I didn't catch much, but I'm here to help. How are you feeling right now?"
                : $"Thanks for sharing. I heard: '{full}'. One small step—take a deep breath and unclench your shoulders.";

            foreach (var token in Tokenize(response))
            {
                await Task.Delay(80);
                yield return token;
            }
        }

        private static IEnumerable<string> Tokenize(string text)
        {
            var parts = text.Split(' ');
            foreach (var p in parts) yield return p + " ";
        }
    }
}
