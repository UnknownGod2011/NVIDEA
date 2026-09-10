using Nvidea.Core.Browser;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Product-facing browser authority boundary. UI/plugin callers receive only the browser operations
/// that preserve NVIDEA's durable-job, exact-approval, download-quarantine and cancellation policy.
/// The underlying <see cref="BrowserHostRuntime"/> remains trusted infrastructure and is never
/// returned to product callers.
/// </summary>
public sealed class BrowserProductRuntime
{
    private readonly BrowserHostRuntime _host;

    internal BrowserProductRuntime(BrowserHostRuntime host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public Task<BrowserJobOutcome> StartActionAsync(
        BrowserAction action,
        CancellationToken cancellationToken = default) =>
        _host.StartActionAsync(action, cancellationToken);

    public Task<BrowserJobOutcome> ApproveAndResumeAsync(
        Guid jobId,
        string exactScope,
        CancellationToken cancellationToken = default) =>
        _host.ApproveAndResumeAsync(jobId, exactScope, cancellationToken);

    public Task<BrowserJobOutcome> CancelAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        _host.CancelAsync(jobId, cancellationToken);

    public Task<IReadOnlyList<BrowserDownloadRecord>> ListDownloadsAsync(
        CancellationToken cancellationToken = default) =>
        _host.ListDownloadsAsync(cancellationToken);

    public Task<BrowserDownloadHandoffPlan> PrepareDownloadHandoffAsync(
        Guid downloadId,
        string destinationDirectory,
        CancellationToken cancellationToken = default) =>
        _host.PrepareDownloadHandoffAsync(downloadId, destinationDirectory, cancellationToken);

    public Task<BrowserDownloadExportReceipt> ApproveAndExportDownloadAsync(
        BrowserDownloadHandoffPlan approvedPlan,
        string exactScope,
        CancellationToken cancellationToken = default) =>
        _host.ApproveAndExportDownloadAsync(approvedPlan, exactScope, cancellationToken);

    public Task<BrowserDownloadDiscardPlan> PrepareDownloadDiscardAsync(
        Guid downloadId,
        CancellationToken cancellationToken = default) =>
        _host.PrepareDownloadDiscardAsync(downloadId, cancellationToken);

    public Task<BrowserDownloadDiscardReceipt> ApproveAndDiscardDownloadAsync(
        BrowserDownloadDiscardPlan approvedPlan,
        string exactScope,
        CancellationToken cancellationToken = default) =>
        _host.ApproveAndDiscardDownloadAsync(approvedPlan, exactScope, cancellationToken);
}
