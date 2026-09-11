namespace Nvidea.Core.Desktop;

/// <summary>
/// Credential-safe startup/readiness projection for the desktop research stack. This type reports
/// capability state and missing configuration names only; it never returns secret values, PEM
/// material, provider identifiers, research payloads, or exception text from provider SDKs.
/// </summary>
public sealed record DesktopResearchReadiness(
    bool LocalResearchReady,
    bool NebiusLifecycleRequested,
    bool NebiusLifecycleReady,
    bool NebiusDispatchRequested,
    bool NebiusDispatchReady,
    IReadOnlyList<string> Blockers)
{
    private static readonly string[] RequiredNebiusLifecycleEnvironment =
    {
        "NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN",
        "NVIDEA_LIVE_SERVERLESS_PROJECT_ID",
        "NVIDEA_LIVE_WORKER_IMAGE",
        "NVIDEA_LIVE_SUBNET_ID",
        "NVIDEA_LIVE_PLATFORM",
        "NVIDEA_LIVE_PRESET",
        "NVIDEA_LIVE_TIMEOUT",
        "NVIDEA_LIVE_DISK_TYPE",
        "NVIDEA_LIVE_DISK_SIZE_BYTES",
        "NVIDEA_LIVE_TRANSPORT_SOURCE",
        "NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE",
        "NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE",
        "NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT",
        "NVIDEA_LIVE_OBJECT_STORAGE_REGION",
        "NVIDEA_LIVE_OBJECT_STORAGE_BUCKET",
        "NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID",
        "NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY",
        "NVIDEA_LIVE_SECRET_NEBIUS_API_KEY_ID",
        "NVIDEA_LIVE_SECRET_TAVILY_API_KEY_ID",
        "NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_ID"
    };

    public static DesktopResearchReadiness InspectEnvironment(
        DesktopResearchCloudMode cloudMode,
        Func<string, string?>? environmentReader = null)
    {
        environmentReader ??= Environment.GetEnvironmentVariable;
        var localReady = HasValue(environmentReader("TAVILY_API_KEY"));
        var blockers = new List<string>();

        if (!localReady)
            blockers.Add("Local research: TAVILY_API_KEY is missing.");

        if (cloudMode.LifecycleEnabled)
        {
            foreach (var name in RequiredNebiusLifecycleEnvironment)
            {
                if (!HasValue(environmentReader(name)))
                    blockers.Add($"Nebius lifecycle: {name} is missing.");
            }
        }

        if (cloudMode.DispatchEnabled && !localReady)
            blockers.Add("New Nebius dispatch: local Tavily-backed research is unavailable.");

        return new DesktopResearchReadiness(
            LocalResearchReady: localReady,
            NebiusLifecycleRequested: cloudMode.LifecycleEnabled,
            NebiusLifecycleReady: false,
            NebiusDispatchRequested: cloudMode.DispatchEnabled,
            NebiusDispatchReady: false,
            blockers);
    }

    public DesktopResearchReadiness WithRuntimeState(
        bool lifecycleReady,
        bool dispatchReady,
        bool cloudPreflightFailed = false)
    {
        var blockers = Blockers.ToList();
        if (cloudPreflightFailed && NebiusLifecycleRequested)
        {
            blockers.Add(
                "Nebius lifecycle: configuration or provider preflight failed; capability remains locked. " +
                "Run the Nebius contract probe for redacted deployment diagnostics.");
        }

        return this with
        {
            NebiusLifecycleReady = NebiusLifecycleRequested && lifecycleReady,
            NebiusDispatchReady = NebiusDispatchRequested && lifecycleReady && dispatchReady,
            Blockers = blockers.Distinct(StringComparer.Ordinal).ToArray()
        };
    }

    public string ToStatusText()
    {
        static string State(bool requested, bool ready) => ready ? "ready" : requested ? "blocked" : "locked";

        var local = LocalResearchReady ? "ready" : "blocked";
        var lifecycle = State(NebiusLifecycleRequested, NebiusLifecycleReady);
        var dispatch = State(NebiusDispatchRequested, NebiusDispatchReady);
        var summary = $"Local research: {local} · Nebius lifecycle: {lifecycle} · New Nebius dispatch: {dispatch}.";

        return Blockers.Count == 0
            ? summary
            : summary + " " + string.Join(" ", Blockers.Take(3));
    }

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);
}
