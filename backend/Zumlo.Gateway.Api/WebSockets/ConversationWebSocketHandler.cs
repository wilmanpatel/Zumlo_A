
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Zumlo.Gateway.Application;
using Zumlo.Gateway.Application.Audio;
using Zumlo.Gateway.Domain;
using Zumlo.Gateway.Domain.Messages;

namespace Zumlo.Gateway.Api.WebSockets
{
    public class ConversationWebSocketHandler
    {
        private readonly SessionManager _sessions;
        private readonly TranscriptionSimulator _transcriber;
        private readonly AssistantResponseSimulator _assistant;
        private readonly ILogger<ConversationWebSocketHandler> _logger;
      
        public ConversationWebSocketHandler(SessionManager sessions, TranscriptionSimulator transcriber, AssistantResponseSimulator assistant, ILogger<ConversationWebSocketHandler> logger)
        {
            _sessions = sessions;
            _transcriber = transcriber;
            _assistant = assistant;
            _logger = logger;
           

        }

        public async Task HandleAsync(HttpContext http, WebSocket socket)
        {

            // Simple auth for WS: accept any non-empty token via query string
            var token = http.Request.Query["token"].ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Missing token", CancellationToken.None);
                return;
            }

            var buffer = new byte[64 * 1024];

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer: buffer, cancellationToken: CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) break;

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                try
                {
                    
                    var envelope = JsonSerializer.Deserialize<MessageEnvelope>(message);
                    if (envelope == null || string.IsNullOrWhiteSpace(envelope.Type))
                        continue;

                    switch (envelope.Type)
                    {
                        case MessageTypes.SessionStart:
                            {
                                var payload = envelope.Payload.Deserialize<SessionStartMessage>();
                                if (payload == null) break;
                                var session = _sessions.Create(payload.sessionId, http.User.Identity?.Name ?? "anon");
                                await SendAsync(socket, new { type = "session.started", payload = new { sessionId = session.Id, state = session.State.ToString() } });
                                break;
                            }
                        case MessageTypes.AudioChunk:
                            {

                                
                                var payload = envelope.Payload.Deserialize<AudioChunkMessage>();
                              
                                if (payload == null || payload.sessionId == null) break;
                                _sessions.AppendAudio(payload.sessionId, payload.sequence, payload.base64Data);
                                // PII-aware: don't log raw data
                                _logger.LogInformation("Audio chunk seq {Seq} len {Len}", payload.sequence, payload.base64Data?.Length);
                                await SendAsync(socket, new { type = "audio.ack", payload = new { sequence = payload.sequence } });
                                break;
                            }
                        case MessageTypes.AudioEnd:
                            {
                                var payload = envelope.Payload.Deserialize<AudioEndMessage>();
                                if (payload == null) break;
                                _sessions.AudioCompleted(payload.sessionId);
                                
                                #region faketext
                                
                                // Stream transcript partials
                                await foreach (var seg in _transcriber.StreamTranscriptAsync(payload.sessionId, _sessions))
                                {
                                    await SendAsync(socket, new { type = MessageTypes.TranscriptPartial, payload = seg });
                                }


                                #endregion


                                #region "OpenAI"
                                //List<byte> audioBuffer = new();
                                //var session = _sessions.GetOrThrow(payload.sessionId);
                                //audioBuffer.AddRange(session.AudioBuffer);
                                //audioBuffer.ToArray();

                                //var stt = new WhisperSpeechToTextEngine("KEY");


                                //var transcript = await stt.TranscribeAsync(audioBuffer);

                                #endregion


                                #region "Azure Speech"
                                //// 1) Get full session audio
                                //var session = _sessions.GetOrThrow(payload.sessionId);

                                //// 2) Call Azure STT
                                //var transcript = await _speechToText.TranscribeAsync(session.AudioBuffer);

                                //// 3) Stream partial segments back (word-by-word)
                                //var words = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                //double t = 0;

                                //foreach (var w in words)
                                //{
                                //    var seg = new TranscriptSegment
                                //    {
                                //        Text = w,
                                //        StartMs = t,
                                //        EndMs = t + 180
                                //    };

                                //    _sessions.AddTranscriptSegment(payload.sessionId, seg);

                                //    await SendAsync(socket, new
                                //    {
                                //        type = MessageTypes.TranscriptPartial,
                                //        payload = seg
                                //    });

                                //    t += 180;
                                //    await Task.Delay(60);
                                //}

                                #endregion


                                // Assistant response streaming
                                await foreach (var delta in _assistant.StreamResponseAsync(payload.sessionId, _sessions))
                                {
                                    await SendAsync(socket, new { type = MessageTypes.AssistantDelta, payload = new { text = delta } });
                                }

                                await SendAsync(socket, new { type = MessageTypes.AssistantComplete, payload = new { } });
                                _sessions.MarkCompleted(payload.sessionId);
                                await SendAsync(socket, new { type = "session.completed", payload = new { sessionId = payload.sessionId } });
                                break;
                            }
                        case MessageTypes.SessionEnd:
                            {
                                var payload = envelope.Payload.Deserialize<SessionEndMessage>();
                                if (payload == null) break;
                                _sessions.End(payload.sessionId);
                                await SendAsync(socket, new { type = "session.ended", payload = new { sessionId = payload.sessionId } });
                                break;
                            }
                        default:
                            await SendAsync(socket, new { type = "error", payload = new { code = "unknown_type", message = envelope.Type } });
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WebSocket message handling failed");
                    await SendAsync(socket, new { type = "error", payload = new { code = "bad_request", message = ex.Message } });
                }
            }

            if (socket.State != WebSocketState.Closed)
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
        }

        private static Task SendAsync(WebSocket socket, object obj)
        {
            var json = JsonSerializer.Serialize(obj);
            var bytes = Encoding.UTF8.GetBytes(json);
            return socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }



    }
}
