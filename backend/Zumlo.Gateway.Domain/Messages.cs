
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zumlo.Gateway.Domain.Messages
{
    public static class MessageTypes
    {
        public const string SessionStart = "session.start";
        public const string AudioChunk = "audio.chunk";
        public const string AudioEnd = "audio.end";
        public const string TranscriptPartial = "transcript.partial";
        public const string AssistantDelta = "assistant.delta";
        public const string AssistantComplete = "assistant.complete";
        public const string SessionEnd = "session.end";
    }

    public record MessageEnvelope
    {
        [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;
        [JsonPropertyName("payload")] public JsonElement Payload { get; init; }
    }

    public record SessionStartMessage
    {
        public string sessionId { get; init; } = Guid.NewGuid().ToString("N");
    }

    public record AudioChunkMessage
    {
        public string sessionId { get; init; } = string.Empty;
        public int sequence { get; init; }
        public string base64Data { get; init; } = string.Empty;
    }

    public record AudioEndMessage
    {
        public string sessionId { get; init; } = string.Empty;
    }

    public record SessionEndMessage
    {
        public string sessionId { get; init; } = string.Empty;
    }
}
