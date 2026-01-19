
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;
using Zumlo.Gateway.Domain;
using Zumlo.Gateway.Infrastructure;

namespace Zumlo.Gateway.Application
{
    public class SessionManager
    {
        private readonly ISessionStore _store;
        private readonly ILogger<SessionManager> _logger;
        private const int MaxAudioBytes = 3 * 1024 * 1024; // 3 MB per session for exercise

        public SessionManager(ISessionStore store, ILogger<SessionManager> logger)
        {
            _store = store;
            _logger = logger;
        }

        public Session Create(string? id, string userId)
        {
            var session = new Session
            {
                Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id!,
                UserId = userId,
                State = SessionState.Created,
            };
            _store.Save(session);
            return session;
        }

        public void AppendAudio(string sessionId, int sequence, string base64)
        {
            var s = GetOrThrow(sessionId);
            //if (s.AudioClosed) throw new InvalidOperationException("Audio already closed");
            if (sequence != s.LastSequence + 1) throw new InvalidOperationException($"Bad sequence: {sequence}, expected {s.LastSequence + 1}");
            var bytes = Convert.FromBase64String(base64);
            if (s.AudioBuffer.Length + bytes.Length > MaxAudioBytes) throw new InvalidOperationException("Audio buffer limit exceeded");

            var merged = new byte[s.AudioBuffer.Length + bytes.Length];
            Buffer.BlockCopy(s.AudioBuffer, 0, merged, 0, s.AudioBuffer.Length);
            Buffer.BlockCopy(bytes, 0, merged, s.AudioBuffer.Length, bytes.Length);
            s.AudioBuffer = merged;
            s.LastSequence = sequence;
            _store.Save(s);
        }

        public void AudioCompleted(string sessionId)
        {
            var s = GetOrThrow(sessionId);
            s.AudioClosed = true;
            s.State = SessionState.Processing;
            _store.Save(s);
        }

        public void AddTranscriptSegment(string sessionId, TranscriptSegment seg)
        {
            var s = GetOrThrow(sessionId);
            s.Segments.Add(seg);
            _store.Save(s);
        }

        public void MarkResponding(string sessionId)
        {
            var s = GetOrThrow(sessionId);
            s.State = SessionState.Responding;
            _store.Save(s);
        }

        public void MarkCompleted(string sessionId)
        {
            var s = GetOrThrow(sessionId);
            s.State = SessionState.Completed;
            _store.Save(s);
        }

        public void End(string sessionId)
        {
            var s = GetOrThrow(sessionId);
            s.State = SessionState.Ended;
            _store.Save(s);
        }

        public Session GetOrThrow(string id)
        {
            var s = _store.Get(id);
            if (s == null) throw new KeyNotFoundException("Session not found");
            return s;
        }

        public Domain.SessionSummary? GetSummary(string id)
        {
            var s = _store.Get(id);
            return (s == null) ? null : _store.GetSummary(id);
        }
    }
}
