using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

public partial class MainWindow
{
    private const int VoiceHotKeyId = 0x4E57;
    private const uint VkV = 0x56;

    private readonly ILocalVoiceTranscriber _voiceTranscriber = new SystemSpeechLocalTranscriber();
    private CancellationTokenSource? _voiceCts;
    private Button? _voiceButton;
    private HwndSource? _voiceSource;
    private bool _voiceRunning;
    private bool _voiceWindowHooksAttached;

    private void InitializeVoiceUi()
    {
        if (_voiceButton is not null || Content is not Grid root)
            return;

        var commandRow = root.Children
            .OfType<Grid>()
            .FirstOrDefault(child => Grid.GetRow(child) == 2);
        if (commandRow is null || commandRow.ColumnDefinitions.Count < 5)
            return;

        commandRow.ColumnDefinitions.Insert(3, new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(StopButton, 4);
        Grid.SetColumn(InvokeButton, 5);

        _voiceButton = new Button
        {
            Content = "Voice",
            Padding = new Thickness(14, 6, 14, 6),
            Margin = new Thickness(0, 0, 8, 0),
            ToolTip = "Ctrl+Shift+V. One-shot local Windows speech recognition; audio is not sent to a cloud speech service and the transcript is reviewed before Run."
        };
        _voiceButton.Click += VoiceButton_Click;
        Grid.SetColumn(_voiceButton, 3);
        commandRow.Children.Add(_voiceButton);

        if (!_voiceWindowHooksAttached)
        {
            StopButton.Click += VoiceEmergencyStop_Click;
            Closed += VoiceWindow_Closed;
            _voiceWindowHooksAttached = true;
        }

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
            return;

        _voiceSource = HwndSource.FromHwnd(handle);
        _voiceSource?.AddHook(VoiceWndProc);
        if (!RegisterHotKey(handle, VoiceHotKeyId, ModControl | ModShift, VkV))
            _voiceButton.ToolTip += " Global voice hotkey registration failed on this Windows session; the Voice button still works.";
    }

    private IntPtr VoiceWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WmHotKey = 0x0312;
        if (msg != WmHotKey || wParam.ToInt32() != VoiceHotKeyId)
            return IntPtr.Zero;

        // Match the text hotkey privacy boundary: capture the foreground app before NVIDEA activates.
        _pendingContext = WindowsContextCapture.Capture(ClipboardCheck.IsChecked == true);
        UpdateContextLabel(_pendingContext);
        Show();
        Activate();
        handled = true;

        _ = Dispatcher.InvokeAsync(async () => await StartVoiceCaptureAsync());
        return IntPtr.Zero;
    }

    private async void VoiceButton_Click(object sender, RoutedEventArgs e) => await StartVoiceCaptureAsync();

    private async Task StartVoiceCaptureAsync()
    {
        if (_running || _browserRunning || _researchRunning || _voiceRunning)
            return;

        if (!_voiceTranscriber.IsAvailable)
        {
            OutputBox.Text = _voiceTranscriber.UnavailableReason
                ?? "Local Windows speech recognition is unavailable.";
            StatusText.Text = "Voice — unavailable locally";
            return;
        }

        var consent = MessageBox.Show(
            this,
            "NVIDEA will listen to your default microphone for one local, one-shot transcription.\n\n"
            + "Audio is processed by the Windows desktop speech recognizer and is not sent to Nebius, Tavily, or another cloud speech service. "
            + "The transcript will only be placed in the prompt box for your review; it will not run automatically.\n\n"
            + "Continue?",
            "Start local microphone transcription?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information,
            MessageBoxResult.No);

        if (consent != MessageBoxResult.Yes)
        {
            StatusText.Text = "Voice — microphone not started";
            return;
        }

        _voiceCts?.Dispose();
        _voiceCts = new CancellationTokenSource();
        SetVoiceRunning(true);
        StatusText.Text = "Voice — listening locally; Emergency stop cancels microphone capture";

        try
        {
            var transcript = await _voiceTranscriber.TranscribeOnceAsync(
                TimeSpan.FromSeconds(20),
                _voiceCts.Token);
            var prompt = transcript.PreparePrompt();

            PromptBox.Text = string.IsNullOrWhiteSpace(PromptBox.Text)
                ? prompt
                : $"{PromptBox.Text.TrimEnd()} {prompt}";
            PromptBox.CaretIndex = PromptBox.Text.Length;
            PromptBox.Focus();

            StatusText.Text = transcript.Confidence < 0.35f
                ? "Voice — low-confidence local transcript ready for review; nothing was sent"
                : "Voice — local transcript ready for review; nothing was sent";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Voice — microphone capture stopped";
        }
        catch (TimeoutException)
        {
            OutputBox.Text = "Local voice capture timed out without a usable transcript. Nothing was sent.";
            StatusText.Text = "Voice — timed out locally";
        }
        catch (Exception)
        {
            OutputBox.Text = "Local voice transcription could not complete. Nothing was sent. Check the Windows microphone permission and installed speech language, then retry.";
            StatusText.Text = "Voice — failed safely";
        }
        finally
        {
            _voiceCts?.Dispose();
            _voiceCts = null;
            SetVoiceRunning(false);
            await RefreshResearchAsync();
        }
    }

    private void VoiceEmergencyStop_Click(object sender, RoutedEventArgs e) => _voiceCts?.Cancel();

    private void VoiceWindow_Closed(object? sender, EventArgs e)
    {
        _voiceCts?.Cancel();
        _voiceCts?.Dispose();
        _voiceCts = null;

        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
            _ = UnregisterHotKey(handle, VoiceHotKeyId);
        _voiceSource?.RemoveHook(VoiceWndProc);
        _voiceSource = null;
    }

    private void SetVoiceRunning(bool running)
    {
        _voiceRunning = running;
        if (running)
        {
            InvokeButton.IsEnabled = false;
            if (_voiceButton is not null)
                _voiceButton.IsEnabled = false;
            BrowserButton.IsEnabled = false;
            BrowserUrlBox.IsEnabled = false;
            PromptBox.IsEnabled = false;
            ModeBox.IsEnabled = false;
            ClipboardCheck.IsEnabled = false;
            RecoveryButton.IsEnabled = false;
            ResearchStartButton.IsEnabled = false;
            ResearchResumeButton.IsEnabled = false;
            ResearchDispatchButton.IsEnabled = false;
            ResearchReconcileButton.IsEnabled = false;
            ResearchCancelButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            return;
        }

        UpdateBusyControls();
        if (_voiceButton is not null)
            _voiceButton.IsEnabled = !_running && !_browserRunning && !_researchRunning;
    }
}
