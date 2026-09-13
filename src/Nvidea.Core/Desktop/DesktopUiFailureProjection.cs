namespace Nvidea.Core.Desktop;

public enum DesktopUiFailureSurface
{
    Invocation = 0,
    BrowserAction = 1,
    BrowserRecovery = 2,
    RecoveryDiscovery = 3,
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

            _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unknown desktop failure surface."),
        };
    }
}
