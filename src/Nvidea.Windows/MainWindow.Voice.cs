using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

public partial class MainWindow
{
    private const int VoiceHotKeyId = 0x4E57;
    private const uint VkV = 0x56;
    private readonly SystemSpeechStreamingRecognizer _streamingVoice = new();
    private readonly LocalSpeechOutput _speechOutput = new();
    private CancellationTokenSource? _voiceCts;
    private Button? _voiceButton;
    private ComboBox? _voiceModeBox;
    private HwndSource? _voiceSource;
    private bool _voiceRunning;
    private bool _jarvisSessionActive;
    private bool _jarvisSpeaking;
    private bool _voiceWindowHooksAttached;
    private JarvisConversation? _jarvisConversation;
    private WindowsFocusTarget? _voiceFocusTarget;

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
            ToolTip = "Ctrl+Shift+V. Local Dictation stays local; Smart Dictation and Jarvis send transcript text, never microphone audio, to Nemotron on Nebius."
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
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;
        _voiceSource = HwndSource.FromHwnd(handle);
        _voiceSource?.AddHook(VoiceWndProc);
        if (!RegisterHotKey(handle, VoiceHotKeyId, ModControl | ModShift, VkV))
            _voiceButton.ToolTip += " Global voice hotkey registration failed in this Windows session; the Voice button still works.";
    }

    private IntPtr VoiceWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WmHotKey = 0x0312;
        if (msg != WmHotKey || wParam.ToInt32() != VoiceHotKeyId) return IntPtr.Zero;
        if (_jarvisSessionActive && _jarvisSpeaking)
        {
            _speechOutput.Stop(); // Barge-in: immediately stop local speech, then return to listening.
            handled = true;
            return IntPtr.Zero;
        }
        if (_voiceRunning)
        {
            _voiceCts?.Cancel();
            handled = true;
            return IntPtr.Zero;
        }
        _voiceFocusTarget = WindowsFocusTarget.Capture();
        _pendingContext = WindowsContextCapture.Capture(ClipboardCheck.IsChecked == true);
        UpdateContextLabel(_pendingContext);
        Show(); Activate(); handled = true;
        _ = Dispatcher.InvokeAsync(async () => await StartVoiceCaptureAsync());
        return IntPtr.Zero;
    }

    private async void VoiceButton_Click(object sender, RoutedEventArgs e) => await StartVoiceCaptureAsync();

    private async Task StartVoiceCaptureAsync()
    {
        if (_voiceRunning || _running || _browserRunning || _researchRunning || _memoryMigrationRunning) return;
        if (!_streamingVoice.IsAvailable)
        {
            OutputBox.Text = _streamingVoice.UnavailableReason ?? "Local Windows speech recognition is unavailable.";
            StatusText.Text = "Voice — unavailable locally";
            return;
        }
        var mode = SelectedVoiceMode();
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
            _jarvisSpeaking = false; _jarvisSessionActive = false;
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
            if (_voiceFocusTarget is null) throw new InvalidOperationException("Use the global voice shortcut from the target application for safe text insertion.");
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
        if (_voiceFocusTarget is null) throw new InvalidOperationException("Use the global voice shortcut from the target application for safe text insertion.");
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
        _jarvisConversation ??= _root.CreateJarvisConversation();
        _jarvisSessionActive = true;
        OutputBox.Text = "Jarvis is active. Speak naturally; pause to end each turn. Press Ctrl+Shift+V while Jarvis speaks to interrupt it, or Emergency stop to end the session.";
        while (!cancellationToken.IsCancellationRequested)
        {
            var transcript = await ListenForTurnAsync(VoiceAssistantMode.Jarvis, cancellationToken);
            var text = transcript.PreparePrompt();
            PromptBox.Text = text;
            StatusText.Text = "Jarvis — reasoning with NVIDIA Nemotron";
            var context = _pendingContext ?? WindowsContextCapture.Capture(false);
            var result = await _jarvisConversation.ProcessTurnAsync(text, context, new JarvisWindowsToolExecutor(this, _voiceFocusTarget), cancellationToken);
            OutputBox.Text = result.Response;
            StatusText.Text = "Jarvis — speaking locally";
            _jarvisSpeaking = true;
            try { await _speechOutput.SpeakAsync(result.Response, cancellationToken); }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
            finally { _jarvisSpeaking = false; }
            _pendingContext = WindowsContextCapture.Capture(false);
            _voiceFocusTarget = WindowsFocusTarget.Capture();
        }
    }

    private async Task<LocalVoiceTranscript> ListenForTurnAsync(VoiceAssistantMode mode, CancellationToken cancellationToken)
    {
        StatusText.Text = $"{ModeTitle(mode)} — listening locally";
        var progress = new Progress<string>(partial => { if (!string.IsNullOrWhiteSpace(partial)) OutputBox.Text = $"Listening: {partial}"; });
        return await _streamingVoice.TranscribeTurnAsync(TimeSpan.FromSeconds(35), TimeSpan.FromMilliseconds(900), progress, cancellationToken);
    }

    private void VoiceEmergencyStop_Click(object sender, RoutedEventArgs e) { _speechOutput.Stop(); _voiceCts?.Cancel(); }

    private async void VoiceWindow_Closed(object? sender, EventArgs e)
    {
        _speechOutput.Stop(); _voiceCts?.Cancel(); _voiceCts?.Dispose(); _voiceCts = null;
        if (_jarvisConversation is not null) await _jarvisConversation.DisposeAsync();
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero) _ = UnregisterHotKey(handle, VoiceHotKeyId);
        _voiceSource?.RemoveHook(VoiceWndProc); _voiceSource = null; _speechOutput.Dispose();
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
