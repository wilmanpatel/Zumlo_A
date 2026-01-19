
namespace Zumlo.Gateway.Domain
{
    public enum SessionState
    {
        Created,
        Recording,
        Processing,
        Responding,
        Completed,
        Ended
    }

    public class TranscriptSegment
    {
        public required string Text { get; set; }
        public required double StartMs { get; set; }
        public required double EndMs { get; set; }
    }

    public class SessionSummary
    {
        public string SessionId { get; set; } = string.Empty;
        public string Transcript { get; set; } = string.Empty;
        public List<string> Themes { get; set; } = new();
        public List<string> MicroActions { get; set; } = new();
    }

    public class Session
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string UserId { get; set; } = string.Empty;
        public SessionState State { get; set; } = SessionState.Created;
        public List<TranscriptSegment> Segments { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public byte[] AudioBuffer { get; set; } = Array.Empty<byte>();
        public int LastSequence { get; set; } = -1;
        public bool AudioClosed { get; set; }
    }
}
