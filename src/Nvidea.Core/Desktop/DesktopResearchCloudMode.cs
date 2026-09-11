namespace Nvidea.Core.Desktop;

/// <summary>
/// Explicit desktop policy for Nebius-backed research lifecycle support. Provider-aware reconciliation
/// can be enabled independently from new paid dispatch so the desktop can safely recover/cancel
/// persisted remote jobs without silently opting users into additional Serverless work.
/// </summary>
internal sealed record DesktopResearchCloudMode(bool LifecycleEnabled, bool DispatchEnabled)
{
    internal const string LifecycleEnvironmentVariable = "NVIDEA_DESKTOP_REMOTE_RESEARCH_LIFECYCLE";
    internal const string DispatchEnvironmentVariable = "NVIDEA_DESKTOP_REMOTE_RESEARCH_DISPATCH";

    internal static DesktopResearchCloudMode FromEnvironment(Func<string, string?>? environmentReader = null)
    {
        environmentReader ??= Environment.GetEnvironmentVariable;
        var lifecycle = ReadBoolean(environmentReader, LifecycleEnvironmentVariable);
        var dispatch = ReadBoolean(environmentReader, DispatchEnvironmentVariable);
        if (dispatch && !lifecycle)
        {
            throw new InvalidOperationException(
                $"{DispatchEnvironmentVariable}=true requires {LifecycleEnvironmentVariable}=true so paid dispatch cannot be enabled without provider lifecycle recovery.");
        }

        return new DesktopResearchCloudMode(lifecycle, dispatch);
    }

    private static bool ReadBoolean(Func<string, string?> environmentReader, string name)
    {
        var raw = environmentReader(name)?.Trim();
        if (string.IsNullOrEmpty(raw))
            return false;
        if (!bool.TryParse(raw, out var parsed))
            throw new InvalidOperationException($"Desktop research configuration '{name}' must be 'true' or 'false'.");
        return parsed;
    }
}
