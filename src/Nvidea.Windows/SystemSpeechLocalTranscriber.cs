using System.Globalization;
using System.Speech.Recognition;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

internal sealed class SystemSpeechLocalTranscriber : ILocalVoiceTranscriber
{
    private readonly RecognizerInfo? _recognizer;

    public SystemSpeechLocalTranscriber()
    {
        _recognizer = SelectRecognizer();
    }

    public bool IsAvailable => _recognizer is not null;

    public string? UnavailableReason => IsAvailable
        ? null
        : "Windows has no installed desktop speech recognizer for the current language.";

    public async Task<LocalVoiceTranscript> TranscribeOnceAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (_recognizer is null)
            throw new InvalidOperationException(UnavailableReason);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        using var recognizer = new SpeechRecognitionEngine(_recognizer);
        recognizer.LoadGrammar(new DictationGrammar());
        recognizer.SetInputToDefaultAudioDevice();

        var completion = new TaskCompletionSource<LocalVoiceTranscript>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        EventHandler<SpeechRecognizedEventArgs>? recognizedHandler = null;
        EventHandler<RecognizeCompletedEventArgs>? completedHandler = null;

        recognizedHandler = (_, args) =>
        {
            var result = args.Result;
            if (result is null || string.IsNullOrWhiteSpace(result.Text))
                return;

            completion.TrySetResult(new LocalVoiceTranscript(
                result.Text,
                result.Confidence,
                result.Culture?.Name ?? _recognizer.Culture.Name));
        };

        completedHandler = (_, args) =>
        {
            if (args.Error is not null)
            {
                completion.TrySetException(args.Error);
                return;
            }

            if (!args.Cancelled)
                completion.TrySetException(new InvalidOperationException("No speech was recognized."));
        };

        recognizer.SpeechRecognized += recognizedHandler;
        recognizer.RecognizeCompleted += completedHandler;

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token);
        using var registration = linkedCts.Token.Register(() =>
        {
            try
            {
                recognizer.RecognizeAsyncCancel();
            }
            catch (InvalidOperationException)
            {
                // Recognition may already have completed between cancellation and this callback.
            }

            completion.TrySetCanceled(linkedCts.Token);
        });

        try
        {
            recognizer.RecognizeAsync(RecognizeMode.Single);
            return await completion.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException("Local voice capture timed out before speech was recognized.");
        }
        finally
        {
            recognizer.SpeechRecognized -= recognizedHandler;
            recognizer.RecognizeCompleted -= completedHandler;
            try
            {
                recognizer.RecognizeAsyncCancel();
            }
            catch (InvalidOperationException)
            {
                // Recognition is already stopped.
            }
        }
    }

    private static RecognizerInfo? SelectRecognizer()
    {
        var installed = SpeechRecognitionEngine.InstalledRecognizers();
        if (installed.Count == 0)
            return null;

        var current = CultureInfo.CurrentUICulture;
        return installed.FirstOrDefault(info =>
                   string.Equals(info.Culture.Name, current.Name, StringComparison.OrdinalIgnoreCase))
               ?? installed.FirstOrDefault(info =>
                   string.Equals(info.Culture.TwoLetterISOLanguageName,
                       current.TwoLetterISOLanguageName,
                       StringComparison.OrdinalIgnoreCase))
               ?? installed[0];
    }
}
