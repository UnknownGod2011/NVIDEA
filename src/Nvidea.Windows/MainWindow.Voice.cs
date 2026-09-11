using System.Windows;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

public partial class MainWindow
{
    private readonly ILocalVoiceTranscriber _voiceTranscriber = new SystemSpeechLocalTranscriber();
    private CancellationTokenSource? _voiceCts;
    private bool _voiceRunning;
    private bool _voiceWindowHooksAttached;

    private async void VoiceButton_Click(object sender, RoutedEventArgs e)
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

        if (!_voiceWindowHooksAttached)
        {
            StopButton.Click += VoiceEmergencyStop_Click;
            Closed += VoiceWindow_Closed;
            _voiceWindowHooksAttached = true;
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
    }

    private void SetVoiceRunning(bool running)
    {
        _voiceRunning = running;
        if (running)
        {
            InvokeButton.IsEnabled = false;
            VoiceButton.IsEnabled = false;
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
    }
}
