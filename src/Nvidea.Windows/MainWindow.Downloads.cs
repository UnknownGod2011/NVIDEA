using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Nvidea.Core.Browser;

namespace Nvidea.Windows;

public partial class MainWindow
{
    private BrowserDownloadSnapshotItem? _downloadCandidate;
    private int _pendingDownloadRecoveryCount;
    private DispatcherTimer? _downloadRefreshTimer;

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        _downloadRefreshTimer ??= new DispatcherTimer(TimeSpan.FromSeconds(4), DispatcherPriority.Background, DownloadRefreshTimer_Tick, Dispatcher);
        _downloadRefreshTimer.Start();
        await RefreshDownloadsAsync();
    }

    private async void DownloadRefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (_running || _browserRunning)
            return;
        await RefreshDownloadsAsync();
    }

    private async Task RefreshDownloadsAsync()
    {
        try
        {
            var snapshot = await _root.LocalState.GetBrowserDownloadSnapshotAsync();
            _pendingDownloadRecoveryCount = snapshot.PendingRecoveryCount;

            // Recovery state always wins over stable retained artifacts. Passive polling never mutates
            // Receiving records; the user must deliberately initialize the trusted browser runtime.
            if (snapshot.HasPendingRecovery)
            {
                _downloadCandidate = null;
                DownloadPanelTitleText.Text = "Download recovery needed";
                DownloadSummaryText.Text =
                    $"{snapshot.PendingRecoveryCount} interrupted/in-progress quarantine record(s) require trusted recovery. " +
                    "No download bytes are exported or deleted by passive inspection.";
                DownloadReviewButton.Content = "Recover safely";
                DownloadPanel.Visibility = Visibility.Visible;
                DownloadReviewButton.IsEnabled = !_running && !_browserRunning;
                DownloadDiscardButton.IsEnabled = false;
                return;
            }

            _downloadCandidate = snapshot.RetainedDownloads.FirstOrDefault();
            if (_downloadCandidate is null)
            {
                DownloadPanel.Visibility = Visibility.Collapsed;
                DownloadPanelTitleText.Text = "Verified download retained in quarantine";
                DownloadSummaryText.Text = string.Empty;
                DownloadReviewButton.Content = "Review & export";
                DownloadReviewButton.IsEnabled = false;
                DownloadDiscardButton.IsEnabled = false;
                return;
            }

            var size = $"{_downloadCandidate.LengthBytes:N0} bytes";
            var state = _downloadCandidate.State == BrowserDownloadState.Exported ? "exported copy retained" : "ready";
            DownloadPanelTitleText.Text = "Verified download retained in quarantine";
            DownloadReviewButton.Content = "Review & export";
            DownloadSummaryText.Text =
                $"{_downloadCandidate.SuggestedFileName} · {_downloadCandidate.SourceHost} · {size} · {state} · " +
                $"quarantine {snapshot.RetainedBytes:N0}/{snapshot.MaxRetainedBytes:N0} bytes";
            DownloadPanel.Visibility = Visibility.Visible;
            DownloadReviewButton.IsEnabled = !_running && !_browserRunning;
            DownloadDiscardButton.IsEnabled = !_running && !_browserRunning;
        }
        catch (Exception ex)
        {
            _downloadCandidate = null;
            _pendingDownloadRecoveryCount = 0;
            DownloadPanel.Visibility = Visibility.Collapsed;
            DownloadReviewButton.IsEnabled = false;
            DownloadDiscardButton.IsEnabled = false;
            StatusText.Text = $"Download quarantine unavailable — {ex.Message}";
        }
    }

    private async Task<BrowserDownloadRecord> ResolveTrustedDownloadAsync(BrowserDownloadSnapshotItem snapshot)
    {
        _browserHost ??= await _root.GetBrowserAsync();
        var records = await _browserHost.ListDownloadsAsync();
        var record = records.FirstOrDefault(item => item.DownloadId == snapshot.DownloadId)
            ?? throw new InvalidOperationException("The selected quarantine download no longer exists after trusted runtime recovery.");

        if (record.State is not (BrowserDownloadState.Ready or BrowserDownloadState.Exported) ||
            record.LengthBytes != snapshot.LengthBytes ||
            !string.Equals(record.Sha256, snapshot.Sha256, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(record.SuggestedFileName, snapshot.SuggestedFileName, StringComparison.Ordinal) ||
            !string.Equals(record.SourceUri.IdnHost, snapshot.SourceHost, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The quarantine download changed after the read-only snapshot. Refresh and review the current trusted state before approving any action.");
        }

        return record;
    }

    private async Task RecoverPendingDownloadsAsync()
    {
        var expectedCount = _pendingDownloadRecoveryCount;
        if (expectedCount <= 0)
            return;

        _browserActionCts?.Dispose();
        _browserActionCts = new CancellationTokenSource();
        var cancellationToken = _browserActionCts.Token;
        SetBrowserRunning(true);
        StatusText.Text = "Download recovery — starting trusted local browser runtime";
        OutputBox.Text = string.Empty;
        try
        {
            _browserHost ??= await _root.GetBrowserAsync(cancellationToken);
            var records = await _browserHost.ListDownloadsAsync(cancellationToken);
            var interrupted = records.Count(static item => item.State == BrowserDownloadState.Interrupted);
            OutputBox.Text =
                $"Trusted quarantine recovery completed.\n\n" +
                $"Records requiring recovery before launch: {expectedCount}\n" +
                $"Interrupted records now recorded: {interrupted}\n\n" +
                "Recovery did not export a file, approve a browser action, or replay a website side effect.";
            StatusText.Text = "Download recovery — reconciled locally";
        }
        catch (OperationCanceledException)
        {
            OutputBox.Text = "Download recovery stopped. No file was exported and no website action was replayed.";
            StatusText.Text = "Download recovery — cancelled safely";
        }
        catch (Exception ex)
        {
            OutputBox.Text = $"Download recovery failed safely. No file was exported and no website action was replayed.\n\n{ex.Message}";
            StatusText.Text = "Download recovery — failed safely";
        }
        finally
        {
            _browserActionCts?.Dispose();
            _browserActionCts = null;
            SetBrowserRunning(false);
            await RefreshDownloadsAsync();
        }
    }

    private async void DownloadReviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_running || _browserRunning)
            return;

        if (_pendingDownloadRecoveryCount > 0 && _downloadCandidate is null)
        {
            await RecoverPendingDownloadsAsync();
            return;
        }

        if (_downloadCandidate is null)
            return;

        var picker = new OpenFolderDialog
        {
            Title = "Choose where NVIDEA may export this verified download",
            Multiselect = false
        };
        if (picker.ShowDialog(this) != true)
            return;

        SetBrowserRunning(true);
        StatusText.Text = "Download — preparing exact approval scope";
        try
        {
            var snapshot = _downloadCandidate;
            var candidate = await ResolveTrustedDownloadAsync(snapshot);
            var plan = await _browserHost!.PrepareDownloadHandoffAsync(candidate.DownloadId, picker.FolderName);

            var dialog = new DownloadHandoffDialog(candidate, plan) { Owner = this };
            if (dialog.ShowDialog() != true)
            {
                StatusText.Text = "Download — export cancelled by user";
                return;
            }

            StatusText.Text = "Download — exporting approved file";
            var receipt = await _browserHost.ApproveAndExportDownloadAsync(
                plan,
                plan.Decision.ApprovalScope ?? throw new InvalidOperationException("Prepared download plan is missing its exact approval scope."));
            OutputBox.Text = $"Verified download exported.\n\n{receipt.DestinationPath}\nSHA-256: {receipt.Sha256}";
            StatusText.Text = "Download — exported after exact approval";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Download — cancelled safely";
        }
        catch (Exception ex)
        {
            OutputBox.Text = $"Download handoff failed safely. No broader file permission was granted.\n\n{ex.Message}";
            StatusText.Text = "Download — failed safely";
        }
        finally
        {
            SetBrowserRunning(false);
            await RefreshDownloadsAsync();
        }
    }

    private async void DownloadDiscardButton_Click(object sender, RoutedEventArgs e)
    {
        if (_running || _browserRunning || _downloadCandidate is null)
            return;

        SetBrowserRunning(true);
        StatusText.Text = "Download — preparing exact discard approval";
        try
        {
            var snapshot = _downloadCandidate;
            var candidate = await ResolveTrustedDownloadAsync(snapshot);
            var plan = await _browserHost!.PrepareDownloadDiscardAsync(candidate.DownloadId);
            var source = candidate.SourceUri.IdnHost;
            var size = candidate.LengthBytes is long bytes ? $"{bytes:N0} bytes" : "size unavailable";
            var hash = candidate.Sha256 ?? "unavailable";

            var decision = MessageBox.Show(
                this,
                $"Permanently discard this verified quarantine copy?\n\n" +
                $"File: {candidate.SuggestedFileName}\n" +
                $"Source: {source}\n" +
                $"Size: {size}\n" +
                $"SHA-256: {hash}\n\n" +
                "This removes only NVIDEA's retained quarantine payload. Any file you already exported remains untouched. " +
                "The approval is single-use and bound to this exact download.",
                "Discard quarantined download",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (decision != MessageBoxResult.Yes)
            {
                StatusText.Text = "Download — discard cancelled by user";
                return;
            }

            StatusText.Text = "Download — discarding approved quarantine payload";
            var receipt = await _browserHost.ApproveAndDiscardDownloadAsync(
                plan,
                plan.Decision.ApprovalScope ?? throw new InvalidOperationException("Prepared discard plan is missing its exact approval scope."));
            OutputBox.Text = $"Quarantine payload discarded after explicit approval.\n\nDownload: {receipt.DownloadId}\nPrevious state: {receipt.PreviousState}";
            StatusText.Text = "Download — discarded after exact approval";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Download — discard cancelled safely";
        }
        catch (Exception ex)
        {
            OutputBox.Text = $"Download discard failed safely. No broader delete permission was granted.\n\n{ex.Message}";
            StatusText.Text = "Download — discard failed safely";
        }
        finally
        {
            SetBrowserRunning(false);
            await RefreshDownloadsAsync();
        }
    }
}
