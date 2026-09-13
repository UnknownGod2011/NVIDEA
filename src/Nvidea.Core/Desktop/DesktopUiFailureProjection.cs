namespace Nvidea.Core.Desktop;

public enum DesktopUiFailureSurface
{
    Invocation = 0,
    BrowserAction = 1,
    BrowserRecovery = 2,
    RecoveryDiscovery = 3,
    DownloadSnapshot = 4,
    DownloadRecovery = 5,
    DownloadExport = 6,
    DownloadDiscard = 7,
    AuditStatus = 8,
}

public sealed record DesktopUiFailureProjection(
    string UserMessage,
    string StatusMessage);

/// <summary>
/// Projects arbitrary local/provider/tool exceptions into fixed product-facing text.
/// Exception details are intentionally ignored so credentials, local paths, provider payloads,
/// prompts, selected text, and other untrusted diagnostics cannot cross into the desktop UI.
/// </summary>
public static class DesktopUiFailureProjector
{
    public static DesktopUiFailureProjection Project(
        DesktopUiFailureSurface surface,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return surface switch
        {
            DesktopUiFailureSurface.Invocation => new(
                "NVIDEA could not complete this request. Provider and tool exception details were withheld. Retry, or inspect the redacted audit/diagnostic surfaces if the failure persists.",
                "Request — failed safely"),

            DesktopUiFailureSurface.BrowserAction => new(
                "Browser action could not complete. Provider, browser, and local exception details were withheld. If Playwright Chromium is not installed, install the browser binaries for Microsoft.Playwright 1.62.0 and retry.",
                "Browser — failed safely"),

            DesktopUiFailureSurface.BrowserRecovery => new(
                "Recovery inspection could not complete. The interrupted action was not retried, and raw browser/provider exception details were withheld.",
                "Recovery — failed safely; human resolution required"),

            DesktopUiFailureSurface.RecoveryDiscovery => new(
                "Recovery state is unavailable. Raw local-store/provider exception details were withheld.",
                "Recovery state unavailable — details withheld"),

            DesktopUiFailureSurface.DownloadSnapshot => new(
                "Download quarantine state is unavailable. No file action was performed, and raw local-store/browser exception details were withheld.",
                "Download quarantine unavailable — details withheld"),

            DesktopUiFailureSurface.DownloadRecovery => new(
                "Download recovery failed safely. No file was exported and no website action was replayed. Raw browser/local exception details were withheld.",
                "Download recovery — failed safely"),

            DesktopUiFailureSurface.DownloadExport => new(
                "Download handoff failed safely. No broader file permission was granted, and raw browser/local exception details were withheld.",
                "Download — failed safely"),

            DesktopUiFailureSurface.DownloadDiscard => new(
                "Download discard failed safely. No broader delete permission was granted, and raw browser/local exception details were withheld.",
                "Download — discard failed safely"),

            DesktopUiFailureSurface.AuditStatus => new(
                "Audit retention status could not be read safely. Raw local-store and diagnostic exception details were withheld.",
                "Audit — read-only status failed safely"),

            _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unknown desktop failure surface."),
        };
    }
}
