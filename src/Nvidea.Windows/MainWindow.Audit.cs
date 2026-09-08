using System.Windows;

namespace Nvidea.Windows;

public partial class MainWindow
{
    private async void AuditStatusButton_Click(object sender, RoutedEventArgs e)
    {
        if (_running || _browserRunning)
            return;

        _browserActionCts?.Dispose();
        _browserActionCts = new CancellationTokenSource();
        var cancellationToken = _browserActionCts.Token;
        SetBrowserRunning(true);
        StatusText.Text = "Audit — reading protected local retention state";

        try
        {
            // Local audit inspection is intentionally browser-free. This path never creates a
            // Playwright runtime or receives browser-action, approval-grant, export, or discard
            // authority merely because the user asked to inspect protected local state.
            var status = await _root.LocalState.GetAuditRetentionStatusAsync(cancellationToken);

            var pruning = status.HasPrunedHistory
                ? $"Older protected history was intentionally retired through segment {status.PrunedThroughSegmentIndex}.\n"
                  + $"Retired events: {status.PrunedEventCount:N0}\n"
                  + $"Protected pruning digest: {status.PrunedAnchorDigest}"
                : "No protected audit history has been pruned.";

            OutputBox.Text =
                "Local audit retention status\n\n"
                + $"Active segment: {status.ActiveSegmentIndex:N0}\n"
                + $"Active events: {status.ActiveEventCount:N0}\n"
                + $"Active on-disk bytes: {FormatBytes(status.ActiveSegmentBytes)}\n\n"
                + $"Archived segments retained: {status.RetainedArchivedSegments:N0} / {status.MaxArchivedSegments:N0}\n"
                + $"Archived bytes retained: {FormatBytes(status.RetainedArchivedBytes)} / {FormatBytes(status.MaxArchivedBytes)}\n"
                + $"Total retained audit bytes: {FormatBytes(status.RetainedTotalBytes)}\n\n"
                + pruning
                + "\n\nThis read-only snapshot intentionally performs no browser launch, audit append, approval, export/discard, repair, or deletion. It also contains no prompts, URLs, filenames, tool arguments, summaries, or other audit payload contents.";
            StatusText.Text = status.HasPrunedHistory
                ? "Audit — read-only retention snapshot; older history has a protected pruning tombstone"
                : "Audit — read-only retention snapshot; no history has been pruned";
        }
        catch (OperationCanceledException)
        {
            OutputBox.Text = "Audit status read stopped.";
            StatusText.Text = "Audit — cancelled";
        }
        catch (Exception ex)
        {
            OutputBox.Text = $"Audit retention status could not be read safely.\n\n{ex.Message}";
            StatusText.Text = "Audit — read-only status failed safely";
        }
        finally
        {
            _browserActionCts?.Dispose();
            _browserActionCts = null;
            SetBrowserRunning(false);
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0)
            return "invalid";
        if (bytes < 1024)
            return $"{bytes:N0} B";
        if (bytes < 1024L * 1024L)
            return $"{bytes / 1024d:N1} KiB";
        if (bytes < 1024L * 1024L * 1024L)
            return $"{bytes / (1024d * 1024d):N1} MiB";
        return $"{bytes / (1024d * 1024d * 1024d):N2} GiB";
    }
}
