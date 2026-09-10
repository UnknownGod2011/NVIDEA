using System.Windows;
using Nvidea.Core.Jobs;

namespace Nvidea.Windows;

public partial class MainWindow
{
    private Guid? _activeResearchJobId;
    private CancellationTokenSource? _researchCts;
    private bool _researchRunning;
    private bool _researchWindowHooksAttached;

    private async void ResearchWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_researchWindowHooksAttached)
        {
            StopButton.Click += ResearchEmergencyStop_Click;
            Closed += ResearchWindow_Closed;
            _researchWindowHooksAttached = true;
        }
        await RefreshResearchAsync();
    }

    private void ResearchEmergencyStop_Click(object sender, RoutedEventArgs e) => _researchCts?.Cancel();

    private void ResearchWindow_Closed(object? sender, EventArgs e)
    {
        _researchCts?.Cancel();
        _researchCts?.Dispose();
        _researchCts = null;
    }

    private async void ResearchStartButton_Click(object sender, RoutedEventArgs e)
    {
        var runtime = _root.Research;
        if (runtime is null)
        {
            ResearchStatusText.Text = "Durable research unavailable — configure TAVILY_API_KEY.";
            return;
        }
        if (_researchRunning || string.IsNullOrWhiteSpace(PromptBox.Text))
            return;

        SetResearchRunning(true);
        try
        {
            var created = await runtime.CreateAsync(PromptBox.Text.Trim());
            _activeResearchJobId = created.JobId;
            await RunResearchStepAsync(runtime, created.JobId);
        }
        catch (Exception)
        {
            ResearchStatusText.Text = "Research could not start. No checkpoint details were exposed.";
        }
        finally
        {
            SetResearchRunning(false);
            await RefreshResearchAsync();
        }
    }

    private async void ResearchResumeButton_Click(object sender, RoutedEventArgs e)
    {
        var runtime = _root.Research;
        if (runtime is null || _activeResearchJobId is not { } jobId || _researchRunning)
            return;

        SetResearchRunning(true);
        try
        {
            var current = await runtime.GetStatusAsync(jobId);
            if (current.RequiresRemoteReconciliation)
            {
                ResearchStatusText.Text = "Remote research cannot be resumed locally. Use Nebius reconciliation when that lifecycle path is enabled.";
                return;
            }

            if (current.CanRecoverInterrupted)
            {
                // Recovery itself never calls Nemotron/Tavily. Re-arming and retrying are two
                // separate deliberate clicks so the duplicate provider-work/cost warning is visible.
                var rearmed = await runtime.RecoverInterruptedAsync(jobId);
                ApplyResearchStatus(rearmed);
                return;
            }

            await RunResearchStepAsync(runtime, jobId);
        }
        catch (Exception)
        {
            ResearchStatusText.Text = "Research step could not run. Durable state was retained for review.";
        }
        finally
        {
            SetResearchRunning(false);
            await RefreshResearchAsync();
        }
    }

    private async void ResearchReconcileButton_Click(object sender, RoutedEventArgs e)
    {
        var runtime = _root.Research;
        if (runtime is null || _activeResearchJobId is not { } jobId || _researchRunning)
            return;
        if (!runtime.RemoteLifecycleAvailable)
        {
            ResearchStatusText.Text = "Nebius lifecycle reconciliation is locked until the validated cloud runtime is composed.";
            return;
        }

        SetResearchRunning(true);
        _researchCts?.Dispose();
        _researchCts = new CancellationTokenSource();
        try
        {
            var current = await runtime.GetStatusAsync(jobId, _researchCts.Token);
            if (!current.RequiresRemoteReconciliation)
            {
                ResearchStatusText.Text = "This research job has no unfinished Nebius lifecycle to reconcile.";
                return;
            }

            var reconciled = await runtime.ReconcileRemoteAsync(jobId, _researchCts.Token);
            ApplyResearchStatus(reconciled);
        }
        catch (OperationCanceledException) when (_researchCts.IsCancellationRequested)
        {
            ResearchStatusText.Text = "Nebius lifecycle reconciliation stopped locally; durable remote state remains authoritative and must be reconciled before retry.";
        }
        catch (Exception)
        {
            ResearchStatusText.Text = "Nebius lifecycle reconciliation could not be confirmed. Local replay remains blocked.";
        }
        finally
        {
            SetResearchRunning(false);
            await RefreshResearchAsync();
        }
    }

    private async void ResearchCancelButton_Click(object sender, RoutedEventArgs e)
    {
        _researchCts?.Cancel();
        var runtime = _root.Research;
        if (runtime is null || _activeResearchJobId is not { } jobId)
            return;

        try
        {
            var current = await runtime.GetStatusAsync(jobId);
            if (current.RequiresRemoteReconciliation && !runtime.RemoteLifecycleAvailable)
            {
                ResearchStatusText.Text = "Remote cancellation is unavailable until the validated Nebius lifecycle runtime is composed. Local fallback is blocked.";
                return;
            }

            var status = await runtime.CancelAsync(jobId);
            ApplyResearchStatus(status);
        }
        catch (Exception)
        {
            ResearchStatusText.Text = "Research cancellation state could not be confirmed.";
        }
        finally
        {
            await RefreshResearchAsync();
        }
    }

    private async Task RunResearchStepAsync(ResearchProductRuntime runtime, Guid jobId)
    {
        _researchCts?.Dispose();
        _researchCts = new CancellationTokenSource();
        var status = await runtime.RunNextLocalStepAsync(jobId, _researchCts.Token);
        ApplyResearchStatus(status);

        if (status.Stage == ResearchJobStage.Completed)
        {
            var report = await runtime.ReadCompletedReportAsync(jobId);
            OutputBox.Text = report.AnswerMarkdown;
        }
    }

    private async Task RefreshResearchAsync()
    {
        var runtime = _root.Research;
        if (runtime is null)
        {
            ResearchPanel.Visibility = Visibility.Collapsed;
            return;
        }

        ResearchPanel.Visibility = Visibility.Visible;
        ResearchCloudStatusText.Text = runtime.RemoteLifecycleAvailable
            ? runtime.RemoteDispatchEnabled
                ? "Nebius lifecycle + new dispatch are enabled in this composition. Consequential cloud execution still requires explicit scoped approval."
                : "Nebius lifecycle reconciliation is available; new Serverless dispatch remains locked."
            : "Cloud execution is locked: local Nemotron + Tavily research is available, but Nebius lifecycle controls remain disabled until the live deployment contract is proven and composed.";

        try
        {
            var statuses = await runtime.ListAsync();
            var active = statuses.FirstOrDefault(static status => !status.IsTerminal)
                ?? statuses.FirstOrDefault();
            if (active is null)
            {
                _activeResearchJobId = null;
                ResearchStatusText.Text = "No durable research job yet.";
                UpdateResearchControls(null);
                return;
            }

            _activeResearchJobId = active.JobId;
            ApplyResearchStatus(active);
            if (active.Stage == ResearchJobStage.Completed)
            {
                var report = await runtime.ReadCompletedReportAsync(active.JobId);
                OutputBox.Text = report.AnswerMarkdown;
            }
        }
        catch (Exception)
        {
            ResearchStatusText.Text = "Durable research status unavailable; checkpoint contents remain hidden.";
            UpdateResearchControls(null);
        }
    }

    private void ApplyResearchStatus(ResearchJobStatus status)
    {
        var location = status.ExecutionLocation == JobExecutionLocation.Local ? "local" : "Nebius Serverless";
        ResearchStatusText.Text = $"{status.DisplayText} · {location} durable execution · attempt {status.Attempt}";
        UpdateResearchControls(status);
    }

    private void SetResearchRunning(bool running)
    {
        _researchRunning = running;
        if (running)
        {
            InvokeButton.IsEnabled = false;
            BrowserButton.IsEnabled = false;
            BrowserUrlBox.IsEnabled = false;
            PromptBox.IsEnabled = false;
            ModeBox.IsEnabled = false;
            ClipboardCheck.IsEnabled = false;
            StopButton.IsEnabled = true;
        }
        else
        {
            UpdateBusyControls();
        }
        UpdateResearchControls(null);
    }

    private void UpdateResearchControls(ResearchJobStatus? status)
    {
        var runtime = _root.Research;
        if (_researchRunning)
        {
            ResearchStartButton.IsEnabled = false;
            ResearchResumeButton.IsEnabled = false;
            ResearchReconcileButton.IsEnabled = false;
            ResearchCancelButton.IsEnabled = true;
            return;
        }

        ResearchStartButton.IsEnabled = runtime is not null;
        ResearchResumeButton.IsEnabled = status is not null
            && !status.RequiresRemoteReconciliation
            && (status.CanRunNextStep || status.CanRecoverInterrupted);
        ResearchResumeButton.Content = status?.CanRecoverInterrupted == true
            ? "Re-arm interrupted stage"
            : "Resume next stage";
        ResearchReconcileButton.IsEnabled = status?.RequiresRemoteReconciliation == true
            && runtime?.RemoteLifecycleAvailable == true;
        ResearchCancelButton.IsEnabled = status?.CanCancel == true
            && (!status.RequiresRemoteReconciliation || runtime?.RemoteLifecycleAvailable == true);
    }
}
