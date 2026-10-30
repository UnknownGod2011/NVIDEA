using System.Globalization;
using System.Speech.Recognition;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

/// <summary>
/// Local continuous Windows speech recognition with partial hypotheses and bounded pause detection.
/// It uses no network service; recognized transcript text is handed to the selected voice mode separately.
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
        var turn = new LocalSpeechTurnAccumulator();

        engine.SpeechHypothesized += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.Result.Text)) return;
            turn.SetHypothesis(args.Result.Text, args.Result.Confidence, DateTimeOffset.UtcNow);
            partialTranscript?.Report(turn.GetTranscript());
        };
        engine.SpeechRecognized += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.Result.Text)) return;
            turn.Commit(args.Result.Text, args.Result.Confidence, DateTimeOffset.UtcNow);
            partialTranscript?.Report(turn.GetTranscript());
        };

        using var maximumCts = new CancellationTokenSource(maximumDuration);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, maximumCts.Token);
        try
        {
            engine.RecognizeAsync(RecognizeMode.Multiple);
            while (true)
            {
                await Task.Delay(100, linkedCts.Token).ConfigureAwait(false);
                if (turn.ShouldFinish(DateTimeOffset.UtcNow, endOfTurnSilence))
                    break;
            }
        }
        catch (OperationCanceledException) when (maximumCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            if (!turn.HasTranscript)
                throw new TimeoutException("No speech was recognized before the local listening timeout.");
        }
        finally
        {
            try { engine.RecognizeAsyncCancel(); } catch (InvalidOperationException) { }
        }

        return new LocalVoiceTranscript(turn.GetTranscript(), turn.Confidence, _recognizer.Culture.Name);
    }

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

internal sealed class LocalSpeechTurnAccumulator
{
    private readonly object _gate = new();
    private readonly List<string> _committed = [];
    private string _hypothesis = string.Empty;
    private float _hypothesisConfidence;
    private float _minimumCommittedConfidence = 1f;
    private bool _hasCommittedConfidence;
    private DateTimeOffset _lastSpeechAt;
    private bool _heardSpeech;

    public float Confidence
    {
        get
        {
            lock (_gate)
                return _hasCommittedConfidence ? _minimumCommittedConfidence : _hypothesisConfidence;
        }
    }

    public bool HasTranscript
    {
        get
        {
            lock (_gate)
                return _committed.Count > 0 || !string.IsNullOrWhiteSpace(_hypothesis);
        }
    }

    public void SetHypothesis(string text, float confidence, DateTimeOffset observedAt)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        lock (_gate)
        {
            _heardSpeech = true;
            _lastSpeechAt = observedAt;
            _hypothesis = text.Trim();
            _hypothesisConfidence = confidence;
        }
    }

    public void Commit(string text, float confidence, DateTimeOffset observedAt)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var normalized = text.Trim();
        lock (_gate)
        {
            _heardSpeech = true;
            _lastSpeechAt = observedAt;
            if (_committed.Count == 0
                || !string.Equals(_committed[^1], normalized, StringComparison.OrdinalIgnoreCase))
                _committed.Add(normalized);
            _minimumCommittedConfidence = _hasCommittedConfidence
                ? Math.Min(_minimumCommittedConfidence, confidence)
                : confidence;
            _hasCommittedConfidence = true;
            _hypothesis = string.Empty;
        }
    }

    public bool ShouldFinish(DateTimeOffset now, TimeSpan endOfTurnSilence)
    {
        lock (_gate)
        {
            var hasText = _committed.Count > 0 || !string.IsNullOrWhiteSpace(_hypothesis);
            return _heardSpeech && hasText && now - _lastSpeechAt >= endOfTurnSilence;
        }
    }

    public string GetTranscript()
    {
        lock (_gate)
            return string.Join(" ", _committed.Append(_hypothesis)
                .Where(static item => !string.IsNullOrWhiteSpace(item)))
                .Trim();
    }
}
