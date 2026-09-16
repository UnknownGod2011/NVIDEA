using Nvidea.Core.Browser;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Memory;
using Nvidea.Core.Nebius;
using Nvidea.Core.Research;
using Nvidea.Core.Security;

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
    private readonly HttpClient? _researchServerlessHttp;
    private readonly NebiusObjectStorageClient? _researchObjectStorage;
    private readonly IAgentInferenceClient _inference;
    private readonly JsonFileMemoryStore _memoryStore;
    private readonly PersonalMemoryService _memory;
    private readonly LocalOllamaMemoryEmbeddingProvider? _memoryEmbeddingProvider;
    private readonly string _stateDirectory;
    private readonly SemaphoreSlim _browserGate = new(1, 1);
    private BrowserHostRuntime? _browser;
    private BrowserProductRuntime? _browserProduct;
    private bool _disposed;

    private NvideaCompositionRoot(
        HttpClient nebiusHttp,
        HttpClient? tavilyHttp,
        HttpClient? researchServerlessHttp,
        NebiusObjectStorageClient? researchObjectStorage,
        IAgentInferenceClient inference,
        JsonFileMemoryStore memoryStore,
        PersonalMemoryService memory,
        LocalOllamaMemoryEmbeddingProvider? memoryEmbeddingProvider,
        DesktopInvocationService desktop,
        DesktopSessionController session,
        ResearchProductRuntime? research,
        string stateDirectory)
    {
        _nebiusHttp = nebiusHttp;
        _tavilyHttp = tavilyHttp;
        _researchServerlessHttp = researchServerlessHttp;
        _researchObjectStorage = researchObjectStorage;
        _inference = inference;
        _memoryStore = memoryStore;
        _memory = memory;
        _memoryEmbeddingProvider = memoryEmbeddingProvider;
        _stateDirectory = stateDirectory;
        Desktop = desktop;
        Session = session;
        Research = research;
        LocalState = new LocalStateRuntime(Path.Combine(stateDirectory, "browser"));
    }

    public DesktopInvocationService Desktop { get; }
    public DesktopSessionController Session { get; }
    public PersonalMemoryService Memory => _memory;

    public ResearchProductRuntime? Research { get; }
    public LocalStateRuntime LocalState { get; }

    public static async Task<NvideaCompositionRoot> CreateFromEnvironmentAsync(
        string? stateDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var dataDirectory = ResolveStateDirectory(stateDirectory);
        Directory.CreateDirectory(dataDirectory);

        var nebiusOptions = NebiusOptions.FromEnvironment();
        var nebiusHttp = ProviderHttpClientFactory.CreateNoRedirectClient();
        var inference = new NebiusTokenFactoryClient(nebiusHttp, nebiusOptions);

        var memoryStore = new JsonFileMemoryStore(Path.Combine(dataDirectory, "memory.json"));
        var memoryEmbeddingConfiguration = LocalMemoryEmbeddingConfiguration.FromEnvironment();
        var memoryEmbeddingProvider = memoryEmbeddingConfiguration.CreateProvider();
        var memory = new PersonalMemoryService(memoryStore, embeddingProvider: memoryEmbeddingProvider);
        await memory.InitializeAsync(cancellationToken).ConfigureAwait(false);

        HttpClient? tavilyHttp = null;
        HttpClient? researchServerlessHttp = null;
        NebiusObjectStorageClient? researchObjectStorage = null;
        ResearchEngine? researchEngine = null;
        ResearchProductRuntime? research = null;
        ILocalResearchRuntime? localResearch = null;
        IResearchCloudExecutionCoordinator? cloudResearch = null;
        var researchDirectory = Path.Combine(dataDirectory, "research");
        var cloudMode = DesktopResearchCloudMode.FromEnvironment();

        var tavilyKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");
        if (!string.IsNullOrWhiteSpace(tavilyKey))
        {
            tavilyHttp = ProviderHttpClientFactory.CreateNoRedirectClient();
            var tavily = new TavilyResearchClient(tavilyHttp, new TavilyOptions { ApiKey = tavilyKey });
            researchEngine = new ResearchEngine(inference, tavily);
            localResearch = new ResearchJobRuntime(researchDirectory, researchEngine);
        }

        if (cloudMode.LifecycleEnabled)
        {
            var configuration = NebiusResearchLiveConfigurationLoader.LoadFromEnvironment();
            try
            {
                var providers = NebiusResearchLiveProviderStartup.CreateAfterDestinationPreflight(
                    configuration,
                    () =>
                    {
                        NebiusObjectStorageClient? objectStorage = null;
                        HttpClient? serverlessHttp = null;
                        try
                        {
                            objectStorage = new NebiusObjectStorageClient(configuration.ObjectStorageOptions);
                            serverlessHttp = ProviderHttpClientFactory.CreateNoRedirectClient();
                            var transport = new S3ProtectedResearchTransport(objectStorage);
                            var serverless = new NebiusServerlessJobClient(
                                serverlessHttp,
                                new NebiusServerlessOptions(
                                    configuration.ServerlessAccessToken,
                                    configuration.ProjectId,
                                    RequestTimeout: TimeSpan.FromSeconds(30),
                                    MaxRetries: 2));
                            return (ObjectStorage: objectStorage, ServerlessHttp: serverlessHttp, Transport: transport, Serverless: serverless);
                        }
                        catch
                        {
                            serverlessHttp?.Dispose();
                            objectStorage?.Dispose();
                            throw;
                        }
                    });

                researchObjectStorage = providers.ObjectStorage;
                researchServerlessHttp = providers.ServerlessHttp;
                var store = new JsonAgentJobStore(Path.Combine(researchDirectory, "research-jobs.json"));
                IAuditTrail audit = new JsonLinesAuditTrail(Path.Combine(researchDirectory, "research-cloud-audit.jsonl"));
                var remote = NebiusResearchLiveRuntimeFactory.Create(
                    store,
                    providers.Serverless,
                    providers.Transport,
                    providers.Transport,
                    providers.Transport,
                    configuration.DispatchOptions,
                    configuration.ClientPrivateKeyPem,
                    audit);
                cloudResearch = new ResearchCloudExecutionCoordinator(researchDirectory, remote);
            }
            catch
            {
                researchServerlessHttp?.Dispose();
                researchObjectStorage?.Dispose();
                researchServerlessHttp = null;
                researchObjectStorage = null;
                throw;
            }
        }

        if (localResearch is not null || cloudResearch is not null)
        {
            research = new ResearchProductRuntime(
                researchDirectory,
                localResearch,
                cloudResearch,
                remoteDispatchEnabled: cloudMode.DispatchEnabled && localResearch is not null);
        }

        var desktop = new DesktopInvocationService(inference, memory, researchEngine);
        var session = new DesktopSessionController(desktop);
        return new NvideaCompositionRoot(
            nebiusHttp,
            tavilyHttp,
            researchServerlessHttp,
            researchObjectStorage,
            inference,
            memoryStore,
            memory,
            memoryEmbeddingProvider,
            desktop,
            session,
            research,
            dataDirectory);
    }

    public async Task<BrowserProductRuntime> GetBrowserProductAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_browserProduct is not null)
            return _browserProduct;

        var host = await GetBrowserHostAsync(cancellationToken).ConfigureAwait(false);
        return _browserProduct ??= new BrowserProductRuntime(host);
    }

    private async Task<BrowserHostRuntime> GetBrowserHostAsync(CancellationToken cancellationToken = default)
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
            var browser = await BrowserHostRuntime.CreateAsync(
                browserDirectory,
                BrowserOptionsFromEnvironment(),
                cancellationToken).ConfigureAwait(false);

            _browser = browser;
            return _browser;
        }
        finally
        {
            _browserGate.Release();
        }
    }

    public async Task<BrowserGoalAgent> CreateBrowserGoalAgentAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var browser = await GetBrowserHostAsync(cancellationToken).ConfigureAwait(false);
        var goalStore = CreateBrowserGoalStore();
        var observedHost = new EvidenceObservingBrowserGoalHost(browser);
        return new BrowserGoalAgent(observedHost, new NemotronBrowserPlanner(_inference), goalStore);
    }

    public async Task<IReadOnlyList<BrowserGoalSession>> ListBrowserGoalSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return await CreateBrowserGoalStore().ListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<BrowserAmbiguousRecoveryService> CreateBrowserAmbiguousRecoveryServiceAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var browser = await GetBrowserHostAsync(cancellationToken).ConfigureAwait(false);
        return new BrowserAmbiguousRecoveryService(browser, CreateBrowserGoalStore());
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
            _browserProduct = null;
        }
        finally
        {
            _browserGate.Release();
        }

        Session.Dispose();
        _memory.Dispose();
        _memoryEmbeddingProvider?.Dispose();
        _memoryStore.Dispose();
        _researchServerlessHttp?.Dispose();
        _researchObjectStorage?.Dispose();
        _tavilyHttp?.Dispose();
        _nebiusHttp.Dispose();
        _browserGate.Dispose();
    }

    private JsonBrowserGoalSessionStore CreateBrowserGoalStore() =>
        new(Path.Combine(_stateDirectory, "browser", "goal-sessions.json"));

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
