using Azure.Core;
using Azure.Identity;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace OrderSphere.Advisory.Api.Voice;

/// <summary>
/// Wraps Azure Cognitive Services Speech for server-side STT and TTS.
/// Disabled (IsEnabled = false) when Speech:Region is absent — callers return 501.
/// Auth uses DefaultAzureCredential; tokens are cached and refreshed 1 minute before expiry.
/// </summary>
public sealed class SpeechService : IDisposable
{
    private static readonly TokenRequestContext SpeechTokenContext =
        new(["https://cognitiveservices.azure.com/.default"]);

    private readonly ILogger<SpeechService> _logger;
    private readonly string? _region;
    // DefaultAzureCredential is not IDisposable; field is intentionally not disposed.
    private readonly DefaultAzureCredential _credential = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    public SpeechService(IConfiguration config, ILogger<SpeechService> logger)
    {
        _logger = logger;
        _region = config["Speech:Region"];
        if (_region is null)
            logger.LogInformation("Speech:Region not configured — voice endpoints disabled");
    }

    public bool IsEnabled => _region is not null;

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
            return _cachedToken;

        await _lock.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
                return _cachedToken;

            var azToken = await _credential.GetTokenAsync(SpeechTokenContext, ct);
            _cachedToken = azToken.Token;
            _tokenExpiry = azToken.ExpiresOn.AddMinutes(-1);
            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Transcribes a 16 kHz / 16-bit / mono WAV file (44-byte header + raw PCM).
    /// Returns an empty string when speech was not detected.
    /// </summary>
    public async Task<string> TranscribeAsync(byte[] wavBytes, CancellationToken ct)
    {
        var token = await GetTokenAsync(ct);
        var config = SpeechConfig.FromAuthorizationToken(token, _region!);
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

        _logger.LogWarning("STT result: {Reason} — details: {Details}", result.Reason, result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult));
        return string.Empty;
    }

    /// <summary>
    /// Synthesizes text to MP3 audio using the de-DE-KatjaNeural voice.
    /// Returns an empty array when synthesis fails.
    /// </summary>
    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken ct)
    {
        var token = await GetTokenAsync(ct);
        var config = SpeechConfig.FromAuthorizationToken(token, _region!);
        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3);
        config.SpeechSynthesisVoiceName = "de-DE-KatjaNeural";

        using var synthesizer = new SpeechSynthesizer(config, null);
        var result = await synthesizer.SpeakTextAsync(text);

        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            return result.AudioData;

        _logger.LogWarning("TTS result: {Reason}", result.Reason);
        return [];
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
