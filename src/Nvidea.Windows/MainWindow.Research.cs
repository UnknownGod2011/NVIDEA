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
        var runtime = _root.ResearchJobs;
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
        var runtime = _root.ResearchJobs;
        if (runtime is null || _activeResearchJobId is not { } jobId || _researchRunning)
            return;

        SetResearchRunning(true);
        try
        {
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

    private async void ResearchCancelButton_Click(object sender, RoutedEventArgs e)
    {
        _researchCts?.Cancel();
        var runtime = _root.ResearchJobs;
        if (runtime is null || _activeResearchJobId is not { } jobId)
            return;

        try
        {
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

    private async Task RunResearchStepAsync(ResearchJobRuntime runtime, Guid jobId)
    {
        _researchCts?.Dispose();
        _researchCts = new CancellationTokenSource();
        var status = await runtime.RunNextStepAsync(jobId, _researchCts.Token);
        ApplyResearchStatus(status);

        if (status.Stage == ResearchJobStage.Completed)
        {
            var report = await runtime.ReadCompletedReportAsync(jobId);
            OutputBox.Text = report.AnswerMarkdown;
        }
    }

    private async Task RefreshResearchAsync()
    {
        var runtime = _root.ResearchJobs;
        if (runtime is null)
        {
            ResearchPanel.Visibility = Visibility.Collapsed;
            return;
        }

        ResearchPanel.Visibility = Visibility.Visible;
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
        if (_researchRunning)
        {
            ResearchStartButton.IsEnabled = false;
            ResearchResumeButton.IsEnabled = false;
            ResearchCancelButton.IsEnabled = true;
            return;
        }

        ResearchStartButton.IsEnabled = true;
        ResearchResumeButton.IsEnabled = status?.CanRunNextStep == true;
        ResearchCancelButton.IsEnabled = status?.CanCancel == true;
    }
}
