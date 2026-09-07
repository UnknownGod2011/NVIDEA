using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;

namespace Nvidea.Windows;

public partial class MainWindow : Window
{
    private const int HotKeyId = 0x4E56;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint VkSpace = 0x20;

    private readonly NvideaCompositionRoot _root;
    private HwndSource? _source;
    private DesktopContext? _pendingContext;
    private BrowserHostRuntime? _browserHost;
    private BrowserAmbiguousRecoveryService? _browserRecovery;
    private BrowserGoalSession? _recoveryCandidate;
    private CancellationTokenSource? _browserActionCts;
    private bool _running;
    private bool _browserRunning;

    public MainWindow(NvideaCompositionRoot root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closed += OnClosed;
        _root.Session.StatusChanged += Session_StatusChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await RefreshRecoveryCandidateAsync();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);

        if (!RegisterHotKey(handle, HotKeyId, ModControl | ModShift, VkSpace))
            StatusText.Text = "Global hotkey unavailable; use the Run button.";
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _browserActionCts?.Cancel();
        _browserActionCts?.Dispose();
        _root.Session.StatusChanged -= Session_StatusChanged;
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
            _ = UnregisterHotKey(handle, HotKeyId);
        _source?.RemoveHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WmHotKey = 0x0312;
        if (msg != WmHotKey || wParam.ToInt32() != HotKeyId)
            return IntPtr.Zero;

        // Capture before activation so context belongs to the app the user invoked NVIDEA from.
        _pendingContext = WindowsContextCapture.Capture(ClipboardCheck.IsChecked == true);
        UpdateContextLabel(_pendingContext);
        Show();
        Activate();
        PromptBox.Focus();
        handled = true;
        return IntPtr.Zero;
    }

    private async void InvokeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_running || _browserRunning || string.IsNullOrWhiteSpace(PromptBox.Text))
            return;

        var allowClipboard = ClipboardCheck.IsChecked == true;
        var context = _pendingContext ?? WindowsContextCapture.Capture(allowClipboard);
        _pendingContext = null;

        // A hotkey capture may have happened before the user toggled clipboard disclosure.
        // Never reuse clipboard text unless disclosure is still explicitly enabled now.
        if (!allowClipboard)
            context = context with { ClipboardText = null };
        else if (string.IsNullOrWhiteSpace(context.ClipboardText))
            context = context with { ClipboardText = WindowsContextCapture.Capture(true).ClipboardText };

        UpdateContextLabel(context);
        var request = new DesktopInvocationRequest(
            PromptBox.Text.Trim(),
            context,
            ResolveMode(),
            allowClipboard);

        SetDesktopRunning(true);
        OutputBox.Text = string.Empty;
        try
        {
            var result = await _root.Session.InvokeAsync(request);
            OutputBox.Text = result.Answer;
        }
        catch (OperationCanceledException)
        {
            OutputBox.Text = "Stopped.";
        }
        catch (Exception ex)
        {
            OutputBox.Text = $"NVIDEA could not complete this request.\n\n{ex.Message}";
        }
        finally
        {
            SetDesktopRunning(false);
        }
    }

    private async void BrowserButton_Click(object sender, RoutedEventArgs e)
    {
        if (_running || _browserRunning)
            return;

        if (!Uri.TryCreate(BrowserUrlBox.Text.Trim(), UriKind.Absolute, out var destination)
            || destination.Scheme is not ("http" or "https"))
        {
            OutputBox.Text = "Browser target must be an absolute HTTP(S) URL.";
            return;
        }

        _browserActionCts?.Dispose();
        _browserActionCts = new CancellationTokenSource();
        var cancellationToken = _browserActionCts.Token;
        SetBrowserRunning(true);
        OutputBox.Text = string.Empty;
        StatusText.Text = "Browser — starting isolated local session";

        try
        {
            _browserHost ??= await _root.GetBrowserAsync(cancellationToken);
            var action = new BrowserAction(
                BrowserActionKind.Navigate,
                Destination: destination,
                Rationale: $"Navigate the user-visible NVIDEA browser to {destination.IdnHost}.");

            var outcome = await _browserHost.StartActionAsync(action, cancellationToken);
            if (outcome.State == AgentJobState.WaitingForApproval && outcome.Approval is not null)
            {
                StatusText.Text = "WaitingForApproval — browser action is paused";
                var dialog = new ApprovalDialog(outcome.Approval) { Owner = this };
                var confirmed = dialog.ShowDialog() == true;

                outcome = confirmed
                    ? await _browserHost.ApproveAndResumeAsync(
                        outcome.JobId,
                        outcome.Approval.ExactScope,
                        cancellationToken)
                    : await _browserHost.CancelAsync(outcome.JobId, CancellationToken.None);
            }

            OutputBox.Text = outcome.Message;
            StatusText.Text = $"Browser — {outcome.State}";
        }
        catch (OperationCanceledException)
        {
            OutputBox.Text = "Browser action stopped.";
            StatusText.Text = "Cancelled";
        }
        catch (Exception ex)
        {
            OutputBox.Text = $"Browser action could not complete.\n\n{ex.Message}\n\nIf Playwright Chromium is not installed, install the browser binaries for Microsoft.Playwright 1.62.0 and retry.";
            StatusText.Text = "Browser — failed safely";
        }
        finally
        {
            _browserActionCts?.Dispose();
            _browserActionCts = null;
            SetBrowserRunning(false);
            await RefreshRecoveryCandidateAsync();
        }
    }

    private async void RecoveryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_running || _browserRunning || _recoveryCandidate is null)
            return;

        _browserActionCts?.Dispose();
        _browserActionCts = new CancellationTokenSource();
        var cancellationToken = _browserActionCts.Token;
        SetBrowserRunning(true);
        StatusText.Text = "Recovery — inspecting fresh browser evidence only";
        OutputBox.Text = string.Empty;

        try
        {
            _browserRecovery ??= await _root.CreateBrowserAmbiguousRecoveryServiceAsync(cancellationToken);
            var (_, recovery) = await _browserRecovery.RecoverAsync(_recoveryCandidate.SessionId, cancellationToken);

            switch (recovery.Status)
            {
                case BrowserAmbiguousRecoveryStatus.Reconciled:
                    var evidenceUrl = recovery.Evidence?.Url.AbsoluteUri;
                    OutputBox.Text = string.IsNullOrWhiteSpace(evidenceUrl)
                        ? $"Recovered without replay.\n\n{recovery.Detail}"
                        : $"Recovered without replay.\n\n{recovery.Detail}\n\nFresh evidence: {evidenceUrl}";
                    StatusText.Text = "Recovery — reconciled automatically from fresh evidence";
                    break;

                case BrowserAmbiguousRecoveryStatus.NeedsHumanResolution:
                    OutputBox.Text = $"Human resolution required. NVIDEA did not retry the interrupted action.\n\n{recovery.Detail}";
                    StatusText.Text = "Recovery — human resolution required; no automatic retry";
                    break;

                default:
                    OutputBox.Text = $"This interrupted job is not currently side-effect ambiguous. No action was replayed.\n\n{recovery.Detail}";
                    StatusText.Text = "Recovery — no ambiguous action to reconcile";
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            OutputBox.Text = "Recovery inspection stopped. No interrupted action was retried.";
            StatusText.Text = "Recovery — cancelled safely";
        }
        catch (Exception ex)
        {
            OutputBox.Text = $"Recovery inspection could not complete. The interrupted action was not retried.\n\n{ex.Message}";
            StatusText.Text = "Recovery — failed safely; human resolution required";
        }
        finally
        {
            _browserActionCts?.Dispose();
            _browserActionCts = null;
            SetBrowserRunning(false);
            await RefreshRecoveryCandidateAsync();
        }
    }

    private async Task RefreshRecoveryCandidateAsync()
    {
        try
        {
            var sessions = await _root.ListBrowserGoalSessionsAsync();
            _recoveryCandidate = sessions
                .Where(static session =>
                    session.PendingJobId is not null
                    && session.Status == BrowserGoalStatus.Failed
                    && session.Detail?.Contains("ambiguous", StringComparison.OrdinalIgnoreCase) == true)
                .OrderByDescending(static session => session.UpdatedAt)
                .FirstOrDefault();

            if (_recoveryCandidate is null)
            {
                RecoveryPanel.Visibility = Visibility.Collapsed;
                RecoverySummaryText.Text = string.Empty;
                return;
            }

            var goal = _recoveryCandidate.Goal;
            if (goal.Length > 180)
                goal = goal[..177] + "...";

            RecoverySummaryText.Text = $"{goal}\nSession {_recoveryCandidate.SessionId:N} · interrupted child {_recoveryCandidate.PendingJobId:N}";
            RecoveryPanel.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            // Recovery discovery is read-only and must never prevent the rest of the desktop shell
            // from starting. Surface the local store problem without initializing/executing browser work.
            _recoveryCandidate = null;
            RecoveryPanel.Visibility = Visibility.Collapsed;
            StatusText.Text = $"Recovery state unavailable — {ex.Message}";
        }
        finally
        {
            UpdateBusyControls();
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _browserActionCts?.Cancel();
        _ = _root.Session.EmergencyStop();
    }

    private void Session_StatusChanged(object? sender, DesktopAgentStatus status)
    {
        if (_browserRunning)
            return;

        Dispatcher.InvokeAsync(() =>
        {
            StatusText.Text = string.IsNullOrWhiteSpace(status.Detail)
                ? status.State.ToString()
                : $"{status.State} — {status.Detail}";
        });
    }

    private DesktopInvocationMode ResolveMode() => ModeBox.SelectedIndex switch
    {
        1 => DesktopInvocationMode.Chat,
        2 => DesktopInvocationMode.Research,
        _ => DesktopInvocationMode.Auto
    };

    private void SetDesktopRunning(bool running)
    {
        _running = running;
        UpdateBusyControls();
    }

    private void SetBrowserRunning(bool running)
    {
        _browserRunning = running;
        UpdateBusyControls();
    }

    private void UpdateBusyControls()
    {
        var busy = _running || _browserRunning;
        InvokeButton.IsEnabled = !busy;
        BrowserButton.IsEnabled = !busy;
        BrowserUrlBox.IsEnabled = !busy;
        PromptBox.IsEnabled = !busy;
        ModeBox.IsEnabled = !busy;
        ClipboardCheck.IsEnabled = !busy;
        RecoveryButton.IsEnabled = !busy && _recoveryCandidate is not null;
        StopButton.IsEnabled = busy;
    }

    private void UpdateContextLabel(DesktopContext context)
    {
        var app = string.IsNullOrWhiteSpace(context.ActiveApplication) ? "unknown app" : context.ActiveApplication;
        var title = string.IsNullOrWhiteSpace(context.WindowTitle) ? string.Empty : $" · {context.WindowTitle}";
        var selection = string.IsNullOrWhiteSpace(context.SelectedText) ? string.Empty : " · selection captured";
        ContextText.Text = $"Context: {app}{title}{selection}";
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
