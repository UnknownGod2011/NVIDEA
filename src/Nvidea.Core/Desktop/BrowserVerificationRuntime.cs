using Nvidea.Core.Browser;
using Nvidea.Core.Security;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Owns the single protected browser-verification evidence pipeline for one desktop host.
/// Host publication and product/judge reads must share this instance so they cannot drift onto
/// different receipt files, protectors, or publication semantics.
/// </summary>
internal sealed class BrowserVerificationRuntime
{
    internal const string ReceiptFileName = "browser-verification-receipt.json.protected";

    private readonly DurableBrowserVerificationPublisher _publisher;

    private BrowserVerificationRuntime(DurableBrowserVerificationPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        Publication = new BrowserVerificationPublicationBoundary(_publisher);
    }

    internal BrowserVerificationPublicationBoundary Publication { get; }

    internal DurableBrowserVerificationPublisher Publisher => _publisher;

    /// <summary>
    /// Production factory. Evidence is protected with Windows CurrentUser DPAPI and stored beneath
    /// the already single-owner browser state directory. No reusable key or approval authority is
    /// persisted by this runtime.
    /// </summary>
    internal static BrowserVerificationRuntime CreateWindows(string stateDirectory)
    {
        if (string.IsNullOrWhiteSpace(stateDirectory))
            throw new ArgumentException("State directory is required.", nameof(stateDirectory));

        var root = Path.GetFullPath(stateDirectory);
        var store = new DurableBrowserVerificationReceiptStore(
            Path.Combine(root, ReceiptFileName),
            new WindowsDpapiLocalStateProtector());
        return new BrowserVerificationRuntime(new DurableBrowserVerificationPublisher(store));
    }

    /// <summary>
    /// Test/composition seam for deterministic protected stores. Keeping construction here still
    /// guarantees that publication and reads use the exact same publisher instance.
    /// </summary>
    internal static BrowserVerificationRuntime Create(
        string receiptPath,
        ILocalStateProtector protector)
    {
        if (string.IsNullOrWhiteSpace(receiptPath))
            throw new ArgumentException("Receipt path is required.", nameof(receiptPath));
        ArgumentNullException.ThrowIfNull(protector);

        var store = new DurableBrowserVerificationReceiptStore(receiptPath, protector);
        return new BrowserVerificationRuntime(new DurableBrowserVerificationPublisher(store));
    }

    internal Task<DesktopBrowserVerificationPresentation> ReadPresentationAsync(
        CancellationToken cancellationToken = default) =>
        _publisher.ReadPresentationAsync(cancellationToken);
}
