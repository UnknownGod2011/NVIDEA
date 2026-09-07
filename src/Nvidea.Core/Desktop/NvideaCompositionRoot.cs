using Nvidea.Core.Browser;
using Nvidea.Core.Memory;
using Nvidea.Core.Nebius;
using Nvidea.Core.Research;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Owns the trusted provider graph for the desktop application. UI/plugin code receives
/// high-level services only; provider credentials and raw clients stay behind this root.
/// Browser automation is created lazily so a missing Playwright browser install cannot
/// prevent chat/research/memory functionality from starting.
/// </summary>
public sealed class NvideaCompositionRoot : IAsyncDisposable
{
    private readonly HttpClient _nebiusHttp;
    private readonly HttpClient? _tavilyHttp;
    private readonly IAgentInferenceClient _inference;
    private readonly JsonFileMemoryStore _memoryStore;
    private readonly PersonalMemoryService _memory;
    private readonly string _stateDirectory;
    private readonly SemaphoreSlim _browserGate = new(1, 1);
    private BrowserHostRuntime? _browser;
    private bool _disposed;

    private NvideaCompositionRoot(
        HttpClient nebiusHttp,
        HttpClient? tavilyHttp,
        IAgentInferenceClient inference,
        JsonFileMemoryStore memoryStore,
        PersonalMemoryService memory,
        DesktopInvocationService desktop,
        DesktopSessionController session,
        string stateDirectory)
    {
        _nebiusHttp = nebiusHttp;
        _tavilyHttp = tavilyHttp;
        _inference = inference;
        _memoryStore = memoryStore;
        _memory = memory;
        _stateDirectory = stateDirectory;
        Desktop = desktop;
        Session = session;
    }

    public DesktopInvocationService Desktop { get; }
    public DesktopSessionController Session { get; }
    public PersonalMemoryService Memory => _memory;

    public static async Task<NvideaCompositionRoot> CreateFromEnvironmentAsync(
        string? stateDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var dataDirectory = ResolveStateDirectory(stateDirectory);
        Directory.CreateDirectory(dataDirectory);

        var nebiusOptions = NebiusOptions.FromEnvironment();
        var nebiusHttp = new HttpClient();
        var inference = new NebiusTokenFactoryClient(nebiusHttp, nebiusOptions);

        var memoryStore = new JsonFileMemoryStore(Path.Combine(dataDirectory, "memory.json"));
        var memory = new PersonalMemoryService(memoryStore);
        await memory.InitializeAsync(cancellationToken).ConfigureAwait(false);

        HttpClient? tavilyHttp = null;
        ResearchEngine? research = null;
        var tavilyKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");
        if (!string.IsNullOrWhiteSpace(tavilyKey))
        {
            tavilyHttp = new HttpClient();
            var tavily = new TavilyResearchClient(tavilyHttp, new TavilyOptions { ApiKey = tavilyKey });
            research = new ResearchEngine(inference, tavily);
        }

        var desktop = new DesktopInvocationService(inference, memory, research);
        var session = new DesktopSessionController(desktop);
        return new NvideaCompositionRoot(
            nebiusHttp,
            tavilyHttp,
            inference,
            memoryStore,
            memory,
            desktop,
            session,
            dataDirectory);
    }

    public async Task<BrowserHostRuntime> GetBrowserAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_browser is not null)
            return _browser;

        await _browserGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_browser is not null)
                return _browser;

            var browserDirectory = Path.Combine(_stateDirectory, "browser");
            _browser = await BrowserHostRuntime.CreateAsync(
                browserDirectory,
                BrowserOptionsFromEnvironment(),
                cancellationToken).ConfigureAwait(false);
            return _browser;
        }
        finally
        {
            _browserGate.Release();
        }
    }

    /// <summary>
    /// Creates the trusted Nemotron-driven browser goal loop over the same local browser host.
    /// Multi-step session state is persisted separately from authorization. Restarting the app
    /// can recover the goal and paused job description, but never recreates an approval grant.
    /// </summary>
    public async Task<BrowserGoalAgent> CreateBrowserGoalAgentAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var browser = await GetBrowserAsync(cancellationToken).ConfigureAwait(false);
        var goalStore = new JsonBrowserGoalSessionStore(Path.Combine(_stateDirectory, "browser", "goal-sessions.json"));
        return new BrowserGoalAgent(browser, new NemotronBrowserPlanner(_inference), goalStore);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        await _browserGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_browser is not null)
                await _browser.DisposeAsync().ConfigureAwait(false);
            _browser = null;
        }
        finally
        {
            _browserGate.Release();
        }

        Session.Dispose();
        _memory.Dispose();
        _memoryStore.Dispose();
        _tavilyHttp?.Dispose();
        _nebiusHttp.Dispose();
        _browserGate.Dispose();
    }

    private static BrowserHostOptions BrowserOptionsFromEnvironment()
    {
        var startRaw = Environment.GetEnvironmentVariable("NVIDEA_BROWSER_START_URL");
        var start = string.IsNullOrWhiteSpace(startRaw)
            ? BrowserHostOptions.Default.StartUri
            : Uri.TryCreate(startRaw.Trim(), UriKind.Absolute, out var parsed)
                ? parsed
                : throw new InvalidOperationException("NVIDEA_BROWSER_START_URL must be an absolute HTTP(S) URL.");

        var allowRaw = Environment.GetEnvironmentVariable("NVIDEA_BROWSER_ALLOWED_HOSTS");
        IReadOnlySet<string>? allowedHosts = null;
        if (!string.IsNullOrWhiteSpace(allowRaw))
        {
            allowedHosts = allowRaw
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(static x => x.ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        var headless = bool.TryParse(Environment.GetEnvironmentVariable("NVIDEA_BROWSER_HEADLESS"), out var parsedHeadless)
            && parsedHeadless;

        return new BrowserHostOptions(start, allowedHosts, headless);
    }

    private static string ResolveStateDirectory(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
            return Path.GetFullPath(requested);

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            throw new InvalidOperationException("Local application data directory is unavailable.");

        return Path.Combine(localAppData, "NVIDEA");
    }
}
