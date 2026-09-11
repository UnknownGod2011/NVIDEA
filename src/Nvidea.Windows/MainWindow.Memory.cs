using System.Windows;
using Nvidea.Core.Memory;

namespace Nvidea.Windows;

public partial class MainWindow
{
    private CancellationTokenSource? _memoryMigrationCts;
    private bool _memoryMigrationRunning;
    private MemoryEmbeddingMigrationPlan? _memoryMigrationPreview;
    private MemoryEmbeddingMigrationOptions? _memoryMigrationPreviewOptions;

    private async void MemoryPreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_running || _browserRunning || _memoryMigrationRunning)
            return;

        await RefreshMemoryMigrationPreviewAsync(showFailureInOutput: true);
    }

    private async void MemoryMigrateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_running || _browserRunning || _memoryMigrationRunning || _memoryMigrationPreview is null)
            return;

        var options = BuildMemoryMigrationOptions();
        if (!Equals(options, _memoryMigrationPreviewOptions))
        {
            InvalidateMemoryMigrationPreview();
            await RefreshMemoryMigrationPreviewAsync(showFailureInOutput: true);
            OutputBox.Text = "Memory maintenance options changed. Review the refreshed privacy-safe preview, then choose Re-index again.";
            return;
        }

        MemoryEmbeddingMigrationPlan freshPlan;
        try
        {
            freshPlan = await _root.Memory.PreviewEmbeddingMigrationAsync(options);
        }
        catch (Exception)
        {
            InvalidateMemoryMigrationPreview();
            OutputBox.Text = "Memory maintenance could not refresh the local re-index plan. Verify local embedding settings/runtime and preview again.";
            return;
        }

        if (!MigrationPlansMatch(_memoryMigrationPreview, freshPlan))
        {
            ApplyMemoryMigrationPreview(freshPlan, options);
            OutputBox.Text = "Memory changed after the previous preview. NVIDEA did not start re-indexing. Review the refreshed counts, then choose Re-index again.";
            return;
        }

        if (freshPlan.TotalCandidates == 0)
        {
            ApplyMemoryMigrationPreview(freshPlan, options);
            OutputBox.Text = "No durable memories currently need re-indexing for this local embedding target.";
            return;
        }

        var sensitiveIncluded = freshPlan.Candidates.Count(candidate => candidate.Sensitivity == MemorySensitivity.Sensitive);
        var restrictedIncluded = freshPlan.Candidates.Count(candidate => candidate.Sensitivity == MemorySensitivity.Restricted);
        var confirmation = BuildMigrationConfirmation(freshPlan, sensitiveIncluded, restrictedIncluded);
        if (MessageBox.Show(this, confirmation, "Confirm local memory re-index", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
            return;

        _memoryMigrationCts?.Dispose();
        _memoryMigrationCts = new CancellationTokenSource();
        _memoryMigrationRunning = true;
        MemoryMigrationProgressBar.Minimum = 0;
        MemoryMigrationProgressBar.Maximum = Math.Max(1, freshPlan.TotalCandidates);
        MemoryMigrationProgressBar.Value = 0;
        MemoryMigrationProgressBar.Visibility = Visibility.Visible;
        MemoryMigrationProgressText.Text = $"0 / {freshPlan.TotalCandidates} processed";
        MemoryMigrationProgressText.Visibility = Visibility.Visible;
        StatusText.Text = "Memory — local re-index running";
        UpdateBusyControls();

        var progress = new Progress<MemoryEmbeddingMigrationProgress>(value =>
        {
            MemoryMigrationProgressBar.Maximum = Math.Max(1, value.Total);
            MemoryMigrationProgressBar.Value = Math.Min(value.Processed, value.Total);
            MemoryMigrationProgressText.Text = $"{value.Processed} / {value.Total} processed · {value.Updated} updated · {value.SkippedConcurrentChanges} changed concurrently";
        });

        try
        {
            var result = await _root.Memory.MigrateEmbeddingsAsync(options, progress, _memoryMigrationCts.Token);
            OutputBox.Text = result.SkippedConcurrentChanges == 0
                ? $"Local memory re-index complete. {result.Updated} of {result.Planned} planned memories were updated."
                : $"Local memory re-index complete. {result.Updated} of {result.Planned} planned memories were updated; {result.SkippedConcurrentChanges} were left untouched because they changed concurrently.";
            StatusText.Text = "Memory — local re-index complete";
        }
        catch (OperationCanceledException)
        {
            OutputBox.Text = "Local memory re-index cancelled. Completed batches remain safely persisted; unfinished memories can be previewed and resumed later.";
            StatusText.Text = "Memory — re-index cancelled safely";
        }
        catch (Exception)
        {
            OutputBox.Text = "Local memory re-index failed safely. Existing memory remains available through normal retrieval fallback. Verify the local embedding runtime/settings and preview again.";
            StatusText.Text = "Memory — re-index failed safely";
        }
        finally
        {
            _memoryMigrationRunning = false;
            _memoryMigrationCts?.Dispose();
            _memoryMigrationCts = null;
            UpdateBusyControls();
            await RefreshMemoryMigrationPreviewAsync(showFailureInOutput: false);
        }
    }

    private void MemoryMigrationCancelButton_Click(object sender, RoutedEventArgs e) =>
        _memoryMigrationCts?.Cancel();

    private void MemoryMigrationPrivacyOption_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || _memoryMigrationRunning)
            return;

        InvalidateMemoryMigrationPreview();
        MemoryMigrationSummaryText.Text = "Privacy scope changed. Preview again before re-indexing.";
        UpdateMemoryMaintenanceControls();
    }

    private async Task RefreshMemoryMigrationPreviewAsync(bool showFailureInOutput)
    {
        var options = BuildMemoryMigrationOptions();
        MemoryEmbeddingMigrationPlan plan;
        try
        {
            plan = await _root.Memory.PreviewEmbeddingMigrationAsync(options);
        }
        catch (InvalidOperationException)
        {
            InvalidateMemoryMigrationPreview();
            MemoryMigrationSummaryText.Text = "Local embedding maintenance unavailable. Enable the local embedding provider before previewing or re-indexing memory.";
            if (showFailureInOutput)
                OutputBox.Text = "Memory re-indexing is local-only and requires a configured migration-capable local embedding provider.";
            UpdateMemoryMaintenanceControls();
            return;
        }
        catch (Exception)
        {
            InvalidateMemoryMigrationPreview();
            MemoryMigrationSummaryText.Text = "Memory maintenance preview unavailable. Existing memory remains unchanged.";
            if (showFailureInOutput)
                OutputBox.Text = "Could not build a privacy-safe local memory maintenance preview. Existing memory was not changed.";
            UpdateMemoryMaintenanceControls();
            return;
        }

        ApplyMemoryMigrationPreview(plan, options);
    }

    private void ApplyMemoryMigrationPreview(MemoryEmbeddingMigrationPlan plan, MemoryEmbeddingMigrationOptions options)
    {
        _memoryMigrationPreview = plan;
        _memoryMigrationPreviewOptions = options;

        var includedSensitive = plan.Candidates.Count(candidate => candidate.Sensitivity == MemorySensitivity.Sensitive);
        var includedRestricted = plan.Candidates.Count(candidate => candidate.Sensitivity == MemorySensitivity.Restricted);
        MemoryMigrationSummaryText.Text =
            $"Local target: {plan.Target.Provider} / {plan.Target.Model} · {plan.TotalCandidates} candidate(s). " +
            $"Included Sensitive: {includedSensitive}; Restricted: {includedRestricted}. " +
            $"Excluded Sensitive: {plan.ExcludedSensitive}; Restricted: {plan.ExcludedRestricted}. No memory content is shown.";

        MemoryMigrationProgressBar.Visibility = Visibility.Collapsed;
        MemoryMigrationProgressText.Visibility = Visibility.Collapsed;
        UpdateMemoryMaintenanceControls();
    }

    private void InvalidateMemoryMigrationPreview()
    {
        _memoryMigrationPreview = null;
        _memoryMigrationPreviewOptions = null;
    }

    private MemoryEmbeddingMigrationOptions BuildMemoryMigrationOptions() => new()
    {
        BatchSize = 8,
        IncludeSensitive = MemoryIncludeSensitiveCheck.IsChecked == true,
        IncludeRestricted = MemoryIncludeRestrictedCheck.IsChecked == true,
    };

    private void UpdateMemoryMaintenanceControls()
    {
        if (!IsInitialized)
            return;

        var otherBusy = _running || _browserRunning;
        MemoryPreviewButton.IsEnabled = !otherBusy && !_memoryMigrationRunning;
        MemoryMigrateButton.IsEnabled = !otherBusy && !_memoryMigrationRunning && _memoryMigrationPreview?.TotalCandidates > 0;
        MemoryMigrationCancelButton.IsEnabled = _memoryMigrationRunning;
        MemoryIncludeSensitiveCheck.IsEnabled = !otherBusy && !_memoryMigrationRunning;
        MemoryIncludeRestrictedCheck.IsEnabled = !otherBusy && !_memoryMigrationRunning;
    }

    private static bool MigrationPlansMatch(MemoryEmbeddingMigrationPlan expected, MemoryEmbeddingMigrationPlan actual)
    {
        if (expected.Target != actual.Target ||
            expected.ExcludedSensitive != actual.ExcludedSensitive ||
            expected.ExcludedRestricted != actual.ExcludedRestricted ||
            expected.Candidates.Count != actual.Candidates.Count)
        {
            return false;
        }

        for (var index = 0; index < expected.Candidates.Count; index++)
        {
            if (expected.Candidates[index] != actual.Candidates[index])
                return false;
        }

        return true;
    }

    private static string BuildMigrationConfirmation(
        MemoryEmbeddingMigrationPlan plan,
        int sensitiveIncluded,
        int restrictedIncluded)
    {
        var elevated = sensitiveIncluded == 0 && restrictedIncluded == 0
            ? "Sensitive and Restricted memories are excluded."
            : $"This run explicitly includes {sensitiveIncluded} Sensitive and {restrictedIncluded} Restricted memory record(s).";

        return $"Re-index {plan.TotalCandidates} durable memory record(s) with the local {plan.Target.Provider} / {plan.Target.Model} embedding target?\n\n" +
               $"{elevated}\n\n" +
               "Memory content will be sent only to the configured local embedding runtime. NVIDEA will update embedding vectors/provenance only, persist completed batches, and leave concurrently changed records untouched.";
    }
}
