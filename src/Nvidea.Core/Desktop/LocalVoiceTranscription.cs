namespace Nvidea.Core.Desktop;

public sealed record LocalVoiceTranscript(
    string Text,
    float Confidence,
    string CultureName)
{
    public string PreparePrompt(int maxCharacters = 4_000)
    {
        if (maxCharacters <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxCharacters));

        if (float.IsNaN(Confidence) || Confidence < 0 || Confidence > 1)
            throw new InvalidOperationException("Voice recognition returned an invalid confidence value.");

        var normalized = Text?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            throw new InvalidOperationException("No speech was recognized.");

        if (normalized.Length > maxCharacters)
            throw new InvalidOperationException(
                $"The local voice transcript exceeded the {maxCharacters:N0}-character review limit.");

        return normalized;
    }
}

/// <summary>
/// Least-authority contract for a local, one-shot microphone transcription implementation.
/// Implementations must not send captured audio to a network service.
/// </summary>
public interface ILocalVoiceTranscriber
{
    bool IsAvailable { get; }

    string? UnavailableReason { get; }

    Task<LocalVoiceTranscript> TranscribeOnceAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
