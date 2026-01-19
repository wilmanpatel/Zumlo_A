using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zumlo.Gateway.Application.Audio
{

    public class SpeechToTextService
    {
        private readonly SpeechConfig _config;
        private readonly ILogger<SpeechToTextService> _logger;

        public SpeechToTextService(
            string subscriptionKey,
            string region,
            ILogger<SpeechToTextService> logger)
        {
            _config = SpeechConfig.FromSubscription(subscriptionKey, region);
            _config.SpeechRecognitionLanguage = "en-US";
            _logger = logger;
        }

        /// <summary>
        /// Converts raw audio bytes (PCM / WAV / WebM) into real text.
        /// </summary>
        public async Task<string> TranscribeAsync(byte[] audioBytes)
        {
            using var audioStream = new BinaryAudioStreamReader(audioBytes);
            var audioFormat = AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1);
            var audioConfig = AudioConfig.FromStreamInput(audioStream, audioFormat);
            using var recognizer = new SpeechRecognizer(_config, audioConfig);

            var result = await recognizer.RecognizeOnceAsync();

            if (result.Reason == ResultReason.RecognizedSpeech)
                return result.Text;

            if (result.Reason == ResultReason.NoMatch)
            {
                _logger.LogWarning("Speech recognition: no match.");
                return "";
            }

            if (result.Reason == ResultReason.Canceled)
            {
                var details = CancellationDetails.FromResult(result);
                _logger.LogError("Speech canceled: {0}", details.ErrorDetails);
            }

            return "";
        }
    }

    /// <summary>
    /// Utility stream so Azure SDK can read byte[] as its own stream.
    /// </summary>
    public class BinaryAudioStreamReader : PullAudioInputStreamCallback
    {
        private readonly byte[] _buffer;
        private int _readPos;

        public BinaryAudioStreamReader(byte[] data)
        {
            _buffer = data;
            _readPos = 0;
        }

        public override int Read(byte[] dataBuffer, uint size)
        {
            int available = _buffer.Length - _readPos;
            if (available <= 0) return 0;

            int toCopy = Math.Min(available, (int)size);
            Array.Copy(_buffer, _readPos, dataBuffer, 0, toCopy);
            _readPos += toCopy;

            return toCopy;
        }
    }

}
