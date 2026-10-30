namespace Nvidea.Core.Tests;

/// <summary>Skips real-browser tests unless the operator explicitly opts in.</summary>
public sealed class BrowserIntegrationFactAttribute : FactAttribute
{
    public BrowserIntegrationFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("NVIDEA_RUN_BROWSER_INTEGRATION"),
                "1",
                StringComparison.Ordinal))
        {
            Skip = "Set NVIDEA_RUN_BROWSER_INTEGRATION=1 after installing Playwright Chromium to run browser integration tests.";
        }
    }
}
