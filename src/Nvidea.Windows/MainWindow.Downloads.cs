using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Nvidea.Core.Browser;

namespace Nvidea.Windows;

public partial class MainWindow
{
    private BrowserDownloadRecord? _downloadCandidate;
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
            _browserHost ??= await _root.GetBrowserAsync();
            var downloads = await _browserHost.ListDownloadsAsync();
            _downloadCandidate = downloads
                .Where(static item => item.State is BrowserDownloadState.Ready or BrowserDownloadState.Exported)
                .OrderByDescending(static item => item.CreatedAt)
                .FirstOrDefault();

            if (_downloadCandidate is null)
            {
                DownloadPanel.Visibility = Visibility.Collapsed;
                DownloadSummaryText.Text = string.Empty;
                DownloadReviewButton.IsEnabled = false;
                DownloadDiscardButton.IsEnabled = false;
                return;
            }

            var source = _downloadCandidate.SourceUri.IdnHost;
            var size = _downloadCandidate.LengthBytes is long bytes ? $"{bytes:N0} bytes" : "size unavailable";
            var state = _downloadCandidate.State == BrowserDownloadState.Exported ? "exported copy retained" : "ready";
            DownloadSummaryText.Text = $"{_downloadCandidate.SuggestedFileName} · {source} · {size} · {state}";
            DownloadPanel.Visibility = Visibility.Visible;
            DownloadReviewButton.IsEnabled = !_running && !_browserRunning;
            DownloadDiscardButton.IsEnabled = !_running && !_browserRunning;
        }
        catch (Exception ex)
        {
            _downloadCandidate = null;
            DownloadPanel.Visibility = Visibility.Collapsed;
            DownloadReviewButton.IsEnabled = false;
            DownloadDiscardButton.IsEnabled = false;
            StatusText.Text = $"Download quarantine unavailable — {ex.Message}";
        }
    }

    private async void DownloadReviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_running || _browserRunning || _downloadCandidate is null)
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
            _browserHost ??= await _root.GetBrowserAsync();
            var candidate = _downloadCandidate;
            var plan = await _browserHost.PrepareDownloadHandoffAsync(candidate.DownloadId, picker.FolderName);

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
            _browserHost ??= await _root.GetBrowserAsync();
            var candidate = _downloadCandidate;
            var plan = await _browserHost.PrepareDownloadDiscardAsync(candidate.DownloadId);
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
