using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Text;
using System.Threading.Channels;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

public partial class MainWindow
{
    private SystemSpeechStreamingRecognizer? _streamingVoice;
    private readonly LocalSpeechOutput _speechOutput = new();
    private CancellationTokenSource? _voiceCts;
    private CancellationTokenSource? _jarvisTurnCts;
    private Button? _voiceButton;
    private ComboBox? _voiceModeBox;
    private bool _voiceRunning;
    private bool _jarvisSessionActive;
    private bool _voiceWindowHooksAttached;
    private JarvisConversation? _jarvisConversation;
    private WindowsFocusTarget? _voiceFocusTarget;

    private SystemSpeechStreamingRecognizer StreamingVoice =>
        _streamingVoice ??= new SystemSpeechStreamingRecognizer(_root.VoicePreferences.SpeechLanguage);

    private void InitializeVoiceUi()
    {
        if (_voiceButton is not null || Content is not Grid root) return;
        var commandRow = root.Children.OfType<Grid>().FirstOrDefault(child => Grid.GetRow(child) == 2);
        if (commandRow is null || commandRow.ColumnDefinitions.Count < 5) return;

        commandRow.ColumnDefinitions.Insert(3, new ColumnDefinition { Width = GridLength.Auto });
        commandRow.ColumnDefinitions.Insert(4, new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(StopButton, 5);
        Grid.SetColumn(InvokeButton, 6);

        _voiceModeBox = new ComboBox { Width = 138, SelectedIndex = 0, Margin = new Thickness(0, 0, 8, 0) };
        _voiceModeBox.Items.Add("Local Dictation");
        _voiceModeBox.Items.Add("Smart Dictation");
        _voiceModeBox.Items.Add("Jarvis");
        Grid.SetColumn(_voiceModeBox, 3);
        commandRow.Children.Add(_voiceModeBox);

        _voiceButton = new Button
        {
            Content = "Voice", Padding = new Thickness(14, 6, 14, 6), Margin = new Thickness(0, 0, 8, 0),
            ToolTip = "Choose a voice mode. Configure separate global shortcuts in Settings."
        };
        _voiceButton.Click += VoiceButton_Click;
        Grid.SetColumn(_voiceButton, 4);
        commandRow.Children.Add(_voiceButton);

        if (!_voiceWindowHooksAttached)
        {
            StopButton.Click += VoiceEmergencyStop_Click;
            Closed += VoiceWindow_Closed;
            _voiceWindowHooksAttached = true;
        }
    }

    private void StartVoiceShortcut(VoiceAssistantMode mode)
    {
        if (_voiceRunning)
        {
            if (mode == VoiceAssistantMode.Jarvis && _jarvisSessionActive && _jarvisTurnCts is not null)
            {
                _speechOutput.Stop();
                _jarvisTurnCts.Cancel(); // Stop the current streamed answer and return to the listening loop.
            }
            else
                _voiceCts?.Cancel();
            return;
        }

        _voiceFocusTarget = WindowsFocusTarget.Capture();
        _pendingContext = WindowsContextCapture.Capture(ClipboardCheck.IsChecked == true);
        UpdateContextLabel(_pendingContext);
        if (_voiceModeBox is not null)
            _voiceModeBox.SelectedIndex = mode switch { VoiceAssistantMode.SmartDictation => 1, VoiceAssistantMode.Jarvis => 2, _ => 0 };
        Show();
        Activate();
        _ = Dispatcher.InvokeAsync(async () => await StartVoiceCaptureAsync(mode));
    }

    private async void VoiceButton_Click(object sender, RoutedEventArgs e) => await StartVoiceCaptureAsync();

    private async Task StartVoiceCaptureAsync(VoiceAssistantMode? requestedMode = null)
    {
        if (_voiceRunning || _running || _browserRunning || _researchRunning || _memoryMigrationRunning) return;
        if (!StreamingVoice.IsAvailable)
        {
            OutputBox.Text = StreamingVoice.UnavailableReason ?? "Local Windows speech recognition is unavailable.";
            StatusText.Text = "Voice — unavailable locally";
            return;
        }
        var mode = requestedMode ?? SelectedVoiceMode();
        var processingNotice = mode == VoiceAssistantMode.LocalDictation
            ? "Your microphone audio is processed locally and is never sent to a cloud speech provider."
            : "Your microphone audio is processed locally. The resulting transcript text may be sent to NVIDIA Nemotron through Nebius for this request.";
        if (MessageBox.Show(this,
                $"{processingNotice}\n\nLocal Dictation inserts exact words. Smart Dictation polishes transcript text. Jarvis keeps a spoken conversation active and requires approval before local actions.\n\nContinue with {ModeTitle(mode)}?",
                "Start voice assistant?", MessageBoxButton.YesNo, MessageBoxImage.Information) != MessageBoxResult.Yes)
        {
            StatusText.Text = "Voice — microphone not started";
            return;
        }

        _voiceCts?.Dispose();
        _voiceCts = new CancellationTokenSource();
        SetVoiceRunning(true);
        try
        {
            if (mode == VoiceAssistantMode.Jarvis) await RunJarvisSessionAsync(_voiceCts.Token);
            else await RunSingleVoiceModeAsync(mode, _voiceCts.Token);
        }
        catch (OperationCanceledException) { StatusText.Text = "Voice — stopped safely"; }
        catch (TimeoutException)
        {
            OutputBox.Text = "No usable speech was detected before the local listening timeout. Nothing was sent or inserted.";
            StatusText.Text = "Voice — timed out locally";
        }
        catch (Exception)
        {
            OutputBox.Text = "Voice could not complete. Check Windows microphone permission and the installed speech language, then retry.";
            StatusText.Text = "Voice — failed safely";
        }
        finally
        {
            _jarvisSessionActive = false;
            _voiceCts?.Dispose(); _voiceCts = null;
            SetVoiceRunning(false);
            await RefreshResearchAsync();
        }
    }

    private async Task RunSingleVoiceModeAsync(VoiceAssistantMode mode, CancellationToken cancellationToken)
    {
        var transcript = await ListenForTurnAsync(mode, cancellationToken);
        var text = transcript.PreparePrompt();
        if (mode == VoiceAssistantMode.LocalDictation)
        {
            if (_voiceFocusTarget is null)
            {
                PromptBox.Text = text;
                OutputBox.Text = "Local Dictation transcribed your speech on this PC. Review or edit the exact transcript above, then run it or copy it where needed.";
                StatusText.Text = "Local Dictation — transcript ready for review";
                return;
            }

            await WindowsTextInsertion.InsertIntoCapturedTargetAsync(_voiceFocusTarget, text, cancellationToken);
            PromptBox.Text = text;
            OutputBox.Text = "Local Dictation inserted the exact local transcript into the captured application.";
            StatusText.Text = "Local Dictation — inserted safely";
            return;
        }

        StatusText.Text = "Smart Dictation — polishing transcript with NVIDIA Nemotron";
        var context = _pendingContext ?? WindowsContextCapture.Capture(false);
        var request = new DesktopInvocationRequest(
            "Rewrite the following dictated text. Preserve every fact, name, number, URL, instruction and intended meaning. Improve grammar, punctuation and formatting only. Return only the polished text.\n\nDICTATION:\n" + text,
            context, DesktopInvocationMode.Chat, false);
        var response = await _root.Session.InvokeAsync(request, cancellationToken);
        var polished = response.Answer.Trim();
        if (string.IsNullOrWhiteSpace(polished)) throw new InvalidOperationException("Nemotron returned no Smart Dictation text.");
        PromptBox.Text = polished;
        if (_voiceFocusTarget is null)
        {
            OutputBox.Text = "Smart Dictation result is ready above for review. It has not been inserted into another application.";
            StatusText.Text = "Smart Dictation — reviewed text ready";
            return;
        }
        if (MessageBox.Show(this, "Insert the reviewed Smart Dictation result into the captured application?", "Smart Dictation approval", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            OutputBox.Text = polished; StatusText.Text = "Smart Dictation — result kept for review"; return;
        }
        await WindowsTextInsertion.InsertIntoCapturedTargetAsync(_voiceFocusTarget, polished, cancellationToken);
        OutputBox.Text = "Smart Dictation inserted the approved Nemotron-polished text.";
        StatusText.Text = "Smart Dictation — inserted safely";
    }

    private async Task RunJarvisSessionAsync(CancellationToken cancellationToken)
    {
        _speechOutput.Configure(_root.VoicePreferences.SpeechOutputVoice, _root.VoicePreferences.SpeechOutputRate);
        _jarvisConversation ??= _root.CreateJarvisConversation();
        _jarvisSessionActive = true;
        OutputBox.Text = "Jarvis is active. Speak naturally; pause to end each turn. Use the Jarvis shortcut while it speaks to interrupt; use Emergency stop to end the session.";
        while (!cancellationToken.IsCancellationRequested)
        {
            var transcript = await ListenForTurnAsync(VoiceAssistantMode.Jarvis, cancellationToken);
            var text = transcript.PreparePrompt();
            PromptBox.Text = text;
            StatusText.Text = "Jarvis — connecting to NVIDIA Nemotron";
            var context = _pendingContext ?? WindowsContextCapture.Capture(false);
            await RunJarvisTurnAsync(text, context, cancellationToken);
            // Keep the invocation context and insertion target pinned for this session.
            // The app owns foreground focus after activation; recapturing here would replace
            // the user's original application with keyboard-wtf itself.
        }
    }

    private async Task RunJarvisTurnAsync(string text, DesktopContext context, CancellationToken sessionToken)
    {
        using var turnCts = CancellationTokenSource.CreateLinkedTokenSource(sessionToken);
        _jarvisTurnCts = turnCts;
        OutputBox.Text = string.Empty;
        var streamedText = new StringBuilder();
        var pendingSpeech = new StringBuilder();
        Channel<string>? speechQueue = null;
        Task? playbackTask = null;

        if (_root.VoicePreferences.SpeakJarvisResponses)
        {
            speechQueue = Channel.CreateBounded<string>(new BoundedChannelOptions(4)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });
            playbackTask = PlayJarvisSpeechQueueAsync(speechQueue.Reader, turnCts.Token);
        }

        try
        {
            var result = await _jarvisConversation!.ProcessTurnStreamingAsync(
                text,
                context,
                new JarvisWindowsToolExecutor(this, _voiceFocusTarget),
                async delta =>
                {
                    streamedText.Append(delta);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        OutputBox.AppendText(delta);
                        OutputBox.ScrollToEnd();
                        StatusText.Text = "Jarvis — response streaming from NVIDIA Nemotron";
                    }).Task.ConfigureAwait(false);

                    if (speechQueue is null) return;
                    pendingSpeech.Append(delta);
                    while (TryTakeSpeakableChunk(pendingSpeech, out var phrase))
                        await speechQueue.Writer.WriteAsync(phrase, turnCts.Token).ConfigureAwait(false);
                },
                turnCts.Token);

            if (speechQueue is not null)
            {
                if (streamedText.Length == 0)
                    pendingSpeech.Append(result.Response);
                if (pendingSpeech.Length > 0)
                    await speechQueue.Writer.WriteAsync(pendingSpeech.ToString(), turnCts.Token).ConfigureAwait(false);
                speechQueue.Writer.TryComplete();
                await playbackTask!.ConfigureAwait(false);
            }

            OutputBox.Text = result.Response;
            StatusText.Text = _root.VoicePreferences.SpeakJarvisResponses
                ? "Jarvis — reply complete; listening locally"
                : "Jarvis — reply ready; spoken output is disabled in Settings";
        }
        catch (OperationCanceledException) when (!sessionToken.IsCancellationRequested)
        {
            speechQueue?.Writer.TryComplete();
            if (playbackTask is not null)
            {
                try { await playbackTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
            StatusText.Text = "Jarvis — response interrupted; listening locally";
        }
        finally
        {
            speechQueue?.Writer.TryComplete();
            _jarvisTurnCts = null;
        }
    }

    private async Task PlayJarvisSpeechQueueAsync(ChannelReader<string> queue, CancellationToken cancellationToken)
    {
        await foreach (var phrase in queue.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            await _speechOutput.SpeakAsync(phrase, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool TryTakeSpeakableChunk(StringBuilder pending, out string chunk)
    {
        chunk = string.Empty;
        var text = pending.ToString();
        var boundary = text.IndexOfAny(['.', '!', '?', '\n']);
        if (boundary >= 0)
        {
            boundary++;
            while (boundary < text.Length && char.IsWhiteSpace(text[boundary])) boundary++;
        }
        else if (text.Length >= 180)
        {
            boundary = text.LastIndexOf(' ', 179);
            if (boundary <= 0) return false;
        }
        else
        {
            return false;
        }

        chunk = text[..boundary].Trim();
        pending.Remove(0, boundary);
        return chunk.Length > 0;
    }

    private async Task<LocalVoiceTranscript> ListenForTurnAsync(VoiceAssistantMode mode, CancellationToken cancellationToken)
    {
        StatusText.Text = $"{ModeTitle(mode)} — listening locally";
        var progress = new Progress<string>(partial => { if (!string.IsNullOrWhiteSpace(partial)) OutputBox.Text = $"Listening: {partial}"; });
        return await StreamingVoice.TranscribeTurnAsync(
            TimeSpan.FromSeconds(_root.VoicePreferences.MaximumListeningSeconds),
            TimeSpan.FromMilliseconds(_root.VoicePreferences.EndOfTurnSilenceMilliseconds),
            progress,
            cancellationToken);
    }

    private void VoiceEmergencyStop_Click(object sender, RoutedEventArgs e) { _speechOutput.Stop(); _voiceCts?.Cancel(); }

    private async void VoiceWindow_Closed(object? sender, EventArgs e)
    {
        _speechOutput.Stop(); _voiceCts?.Cancel(); _voiceCts?.Dispose(); _voiceCts = null;
        if (_jarvisConversation is not null) await _jarvisConversation.DisposeAsync();
        _speechOutput.Dispose();
    }

    private VoiceAssistantMode SelectedVoiceMode() => _voiceModeBox?.SelectedIndex switch { 1 => VoiceAssistantMode.SmartDictation, 2 => VoiceAssistantMode.Jarvis, _ => VoiceAssistantMode.LocalDictation };
    private static string ModeTitle(VoiceAssistantMode mode) => mode switch { VoiceAssistantMode.LocalDictation => "Local Dictation", VoiceAssistantMode.SmartDictation => "Smart Dictation", _ => "Jarvis" };

    private void SetVoiceRunning(bool running)
    {
        _voiceRunning = running;
        if (running)
        {
            InvokeButton.IsEnabled = false;
            if (_voiceButton is not null) _voiceButton.IsEnabled = false;
            if (_voiceModeBox is not null) _voiceModeBox.IsEnabled = false;
            BrowserButton.IsEnabled = false; BrowserUrlBox.IsEnabled = false; PromptBox.IsEnabled = false; ModeBox.IsEnabled = false; ClipboardCheck.IsEnabled = false; RecoveryButton.IsEnabled = false;
            ResearchStartButton.IsEnabled = false; ResearchResumeButton.IsEnabled = false; ResearchDispatchButton.IsEnabled = false; ResearchReconcileButton.IsEnabled = false; ResearchCancelButton.IsEnabled = false; StopButton.IsEnabled = true;
            UpdateMemoryMaintenanceControls(); return;
        }
        UpdateBusyControls();
        if (_voiceButton is not null) _voiceButton.IsEnabled = !_running && !_browserRunning && !_researchRunning && !_memoryMigrationRunning;
        if (_voiceModeBox is not null) _voiceModeBox.IsEnabled = !_running && !_browserRunning && !_researchRunning && !_memoryMigrationRunning;
    }

    private enum VoiceAssistantMode { LocalDictation, SmartDictation, Jarvis }
}
