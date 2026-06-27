using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace OrderSphere.Advisory.Api.Voice;

/// <summary>
/// Wraps Azure Cognitive Services Speech for server-side STT and TTS.
/// Requires Speech:Region and Speech:SubscriptionKey in configuration.
/// Disabled gracefully (IsEnabled = false) when either is absent — callers return 501.
/// </summary>
public sealed class SpeechService
{
    private readonly ILogger<SpeechService> _logger;
    private readonly string? _region;
    private readonly string? _subscriptionKey;

    public SpeechService(IConfiguration config, ILogger<SpeechService> logger)
    {
        _logger = logger;
        _region = config["Speech:Region"];
        _subscriptionKey = config["Speech:SubscriptionKey"];

        if (_region is null || _subscriptionKey is null)
            logger.LogInformation(
                "Speech:Region or Speech:SubscriptionKey not configured — voice endpoints disabled");
    }

    public bool IsEnabled => _region is not null && _subscriptionKey is not null;

    private SpeechConfig CreateConfig()
    {
        var config = SpeechConfig.FromSubscription(_subscriptionKey!, _region!);
        return config;
    }

    /// <summary>
    /// Transcribes a 16 kHz / 16-bit / mono WAV file (44-byte header + raw PCM).
    /// Returns an empty string when speech was not detected.
    /// </summary>
    public async Task<string> TranscribeAsync(byte[] wavBytes, CancellationToken ct)
    {
        var config = CreateConfig();
        config.SpeechRecognitionLanguage = "de-DE";

        using var pushStream = AudioInputStream.CreatePushStream(
            AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
        using var audioConfig = AudioConfig.FromStreamInput(pushStream);
        using var recognizer = new SpeechRecognizer(config, audioConfig);

        // Skip the 44-byte WAV header; the push stream expects raw PCM.
        var pcm = wavBytes.AsSpan(44).ToArray();
        pushStream.Write(pcm);
        pushStream.Close();

        var result = await recognizer.RecognizeOnceAsync();

        if (result.Reason == ResultReason.RecognizedSpeech)
            return result.Text;

        if (result.Reason == ResultReason.Canceled)
        {
            var details = CancellationDetails.FromResult(result);
            _logger.LogWarning("STT canceled: {Code} — {Details}", details.ErrorCode, details.ErrorDetails);
        }
        else
        {
            _logger.LogWarning("STT result: {Reason}", result.Reason);
        }

        return string.Empty;
    }

    /// <summary>
    /// Synthesizes text to MP3 audio using the de-DE-KatjaNeural voice.
    /// Returns an empty array when synthesis fails.
    /// </summary>
    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken ct)
    {
        var config = CreateConfig();
        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3);
        config.SpeechSynthesisVoiceName = "de-DE-KatjaNeural";

        using var synthesizer = new SpeechSynthesizer(config, null);
        var result = await synthesizer.SpeakTextAsync(text);

        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            return result.AudioData;

        if (result.Reason == ResultReason.Canceled)
        {
            var details = SpeechSynthesisCancellationDetails.FromResult(result);
            _logger.LogWarning("TTS canceled: {Code} — {Details}", details.ErrorCode, details.ErrorDetails);
        }
        else
        {
            _logger.LogWarning("TTS result: {Reason}", result.Reason);
        }

        return [];
    }
}
