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
                .Where(static item => item.State == BrowserDownloadState.Ready)
                .OrderByDescending(static item => item.CreatedAt)
                .FirstOrDefault();

            if (_downloadCandidate is null)
            {
                DownloadPanel.Visibility = Visibility.Collapsed;
                DownloadSummaryText.Text = string.Empty;
                DownloadReviewButton.IsEnabled = false;
                return;
            }

            var source = _downloadCandidate.SourceUri.IdnHost;
            var size = _downloadCandidate.LengthBytes is long bytes ? $"{bytes:N0} bytes" : "size unavailable";
            DownloadSummaryText.Text = $"{_downloadCandidate.SuggestedFileName} · {source} · {size}";
            DownloadPanel.Visibility = Visibility.Visible;
            DownloadReviewButton.IsEnabled = !_running && !_browserRunning;
        }
        catch (Exception ex)
        {
            _downloadCandidate = null;
            DownloadPanel.Visibility = Visibility.Collapsed;
            DownloadReviewButton.IsEnabled = false;
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
}
