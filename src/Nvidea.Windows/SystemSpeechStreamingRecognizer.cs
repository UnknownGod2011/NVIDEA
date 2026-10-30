using System.Globalization;
using System.Speech.Recognition;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

/// <summary>
/// Local continuous Windows speech recognition with partial hypotheses and bounded pause detection.
/// It uses no network service and is the default speech path when no optional Speech NIM endpoint is configured.
/// </summary>
internal sealed class SystemSpeechStreamingRecognizer
{
    private readonly RecognizerInfo? _recognizer;

    public SystemSpeechStreamingRecognizer(string? language = null) => _recognizer = SelectRecognizer(language);

    public bool IsAvailable => _recognizer is not null;
    public string? UnavailableReason => IsAvailable ? null : "Windows has no installed desktop speech recognizer for the current language.";

    public async Task<LocalVoiceTranscript> TranscribeTurnAsync(
        TimeSpan maximumDuration,
        TimeSpan endOfTurnSilence,
        IProgress<string>? partialTranscript,
        CancellationToken cancellationToken)
    {
        if (_recognizer is null) throw new InvalidOperationException(UnavailableReason);
        if (maximumDuration <= TimeSpan.Zero || endOfTurnSilence <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maximumDuration));

        using var engine = new SpeechRecognitionEngine(_recognizer);
        engine.LoadGrammar(new DictationGrammar());
        engine.SetInputToDefaultAudioDevice();
        var recognized = new List<string>();
        var gate = new object();
        var lastHypothesis = string.Empty;
        var lastSpeech = DateTimeOffset.UtcNow;
        var heardSpeech = false;

        engine.SpeechHypothesized += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.Result.Text)) return;
            lock (gate)
            {
                heardSpeech = true;
                lastSpeech = DateTimeOffset.UtcNow;
                lastHypothesis = args.Result.Text.Trim();
                partialTranscript?.Report(JoinTranscript(recognized, args.Result.Text));
            }
        };
        engine.SpeechRecognized += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.Result.Text)) return;
            lock (gate)
            {
                heardSpeech = true;
                lastSpeech = DateTimeOffset.UtcNow;
                if (recognized.Count == 0 || !string.Equals(recognized[^1], args.Result.Text, StringComparison.OrdinalIgnoreCase))
                    recognized.Add(args.Result.Text.Trim());
                lastHypothesis = string.Empty;
                partialTranscript?.Report(JoinTranscript(recognized, null));
            }
        };

        using var maximumCts = new CancellationTokenSource(maximumDuration);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, maximumCts.Token);
        try
        {
            engine.RecognizeAsync(RecognizeMode.Multiple);
            while (true)
            {
                await Task.Delay(100, linkedCts.Token).ConfigureAwait(false);
                lock (gate)
                {
                    if (heardSpeech && DateTimeOffset.UtcNow - lastSpeech >= endOfTurnSilence && recognized.Count > 0)
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (maximumCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            lock (gate)
            {
                if (recognized.Count == 0)
                    throw new TimeoutException("No speech was recognized before the local listening timeout.");
            }
        }
        finally
        {
            try { engine.RecognizeAsyncCancel(); } catch (InvalidOperationException) { }
        }

        string text;
        lock (gate) text = JoinTranscript(recognized, lastHypothesis);
        return new LocalVoiceTranscript(text, 1f, _recognizer.Culture.Name);
    }

    private static string JoinTranscript(IEnumerable<string> committed, string? partial) =>
        string.Join(" ", committed.Append(partial ?? string.Empty).Where(static item => !string.IsNullOrWhiteSpace(item))).Trim();

    public static IReadOnlyList<string> GetInstalledLanguages() =>
        SpeechRecognitionEngine.InstalledRecognizers()
            .Select(static info => info.Culture.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static RecognizerInfo? SelectRecognizer(string? language)
    {
        var installed = SpeechRecognitionEngine.InstalledRecognizers();
        if (installed.Count == 0) return null;
        if (!string.IsNullOrWhiteSpace(language)
            && !string.Equals(language, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return installed.FirstOrDefault(info => string.Equals(info.Culture.Name, language, StringComparison.OrdinalIgnoreCase));
        }
        var current = CultureInfo.CurrentUICulture;
        return installed.FirstOrDefault(info => string.Equals(info.Culture.Name, current.Name, StringComparison.OrdinalIgnoreCase))
            ?? installed.FirstOrDefault(info => string.Equals(info.Culture.TwoLetterISOLanguageName, current.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
            ?? installed[0];
    }
}
