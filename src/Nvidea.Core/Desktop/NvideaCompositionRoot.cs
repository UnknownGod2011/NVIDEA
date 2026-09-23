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
    private readonly JsonBrowserGoalSessionStore _browserGoalStore;
    private readonly CompositionLifetimeGate _lifetime = new();
    private readonly Func<CancellationToken, Task<ICrashConsistentBrowserGoalHost>>? _browserGoalHostFactory;
    private BrowserHostRuntime? _browser;
    private BrowserProductRuntime? _browserProduct;

    private NvideaCompositionRoot(HttpClient nebiusHttp, HttpClient? tavilyHttp, HttpClient? researchServerlessHttp, NebiusObjectStorageClient? researchObjectStorage, IAgentInferenceClient inference, JsonFileMemoryStore memoryStore, PersonalMemoryService memory, LocalOllamaMemoryEmbeddingProvider? memoryEmbeddingProvider, DesktopInvocationService desktop, DesktopSessionController session, ResearchProductRuntime? research, string stateDirectory, Func<CancellationToken, Task<ICrashConsistentBrowserGoalHost>>? browserGoalHostFactory = null)
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
        _browserGoalHostFactory = browserGoalHostFactory;
        _browserGoalStore = new(Path.Combine(stateDirectory, "browser", "goal-sessions.json"));
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

    public static async Task<NvideaCompositionRoot> CreateFromEnvironmentAsync(string? stateDirectory = null, CancellationToken cancellationToken = default)
    {
        var dataDirectory = ResolveStateDirectory(stateDirectory);
        Directory.CreateDirectory(dataDirectory);
        using var startupLease = new StartupResourceLease();
        var nebiusOptions = NebiusOptions.FromEnvironment();
        var nebiusHttp = startupLease.Own(ProviderHttpClientFactory.CreateNoRedirectClient());
        var inference = new NebiusTokenFactoryClient(nebiusHttp, nebiusOptions);
        var memoryStore = startupLease.Own(new JsonFileMemoryStore(Path.Combine(dataDirectory, "memory.json")));
        var memoryEmbeddingConfiguration = LocalMemoryEmbeddingConfiguration.FromEnvironment();
        var memoryEmbeddingProvider = memoryEmbeddingConfiguration.CreateProvider();
        if (memoryEmbeddingProvider is not null) startupLease.Own(memoryEmbeddingProvider);
        var memory = startupLease.Own(new PersonalMemoryService(memoryStore, embeddingProvider: memoryEmbeddingProvider));
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
            tavilyHttp = startupLease.Own(ProviderHttpClientFactory.CreateNoRedirectClient());
            var tavily = new TavilyResearchClient(tavilyHttp, new TavilyOptions { ApiKey = tavilyKey });
            researchEngine = new ResearchEngine(inference, tavily);
            localResearch = new ResearchJobRuntime(researchDirectory, researchEngine);
        }

        if (cloudMode.LifecycleEnabled)
        {
            var configuration = NebiusResearchLiveConfigurationLoader.LoadFromEnvironment();
            var providers = NebiusResearchLiveProviderStartup.CreateAfterDestinationPreflight(configuration, () =>
            {
                NebiusObjectStorageClient? objectStorage = null;
                HttpClient? serverlessHttp = null;
                try
                {
                    objectStorage = new NebiusObjectStorageClient(configuration.ObjectStorageOptions);
                    serverlessHttp = ProviderHttpClientFactory.CreateNoRedirectClient();
                    var transport = new S3ProtectedResearchTransport(objectStorage);
                    var serverless = new NebiusServerlessJobClient(serverlessHttp, new NebiusServerlessOptions(configuration.ServerlessAccessToken, configuration.ProjectId, RequestTimeout: TimeSpan.FromSeconds(30), MaxRetries: 2));
                    return (ObjectStorage: objectStorage, ServerlessHttp: serverlessHttp, Transport: transport, Serverless: serverless);
                }
                catch
                {
                    serverlessHttp?.Dispose();
                    objectStorage?.Dispose();
                    throw;
                }
            });
            researchObjectStorage = startupLease.Own(providers.ObjectStorage);
            researchServerlessHttp = startupLease.Own(providers.ServerlessHttp);
            var store = new JsonAgentJobStore(Path.Combine(researchDirectory, "research-jobs.json"));
            IAuditTrail audit = new JsonLinesAuditTrail(Path.Combine(researchDirectory, "research-cloud-audit.jsonl"));
            IRemoteResearchClientRuntime remote = NebiusResearchLiveRuntimeFactory.Create(store, providers.Serverless, providers.Transport, providers.Transport, providers.Transport, configuration.DispatchOptions, configuration.ClientPrivateKeyPem, audit);
            remote = CreateObservedRemoteResearchRuntime(remote);
            cloudResearch = new ResearchCloudExecutionCoordinator(researchDirectory, remote);
        }

        if (localResearch is not null || cloudResearch is not null)
            research = new ResearchProductRuntime(researchDirectory, localResearch, cloudResearch, remoteDispatchEnabled: cloudMode.DispatchEnabled && localResearch is not null);

        var desktop = new DesktopInvocationService(inference, memory, researchEngine);
        var session = new DesktopSessionController(desktop);
        var root = new NvideaCompositionRoot(nebiusHttp, tavilyHttp, researchServerlessHttp, researchObjectStorage, inference, memoryStore, memory, memoryEmbeddingProvider, desktop, session, research, dataDirectory);
        startupLease.ReleaseAll();
        return root;
    }

    internal static IRemoteResearchClientRuntime CreateObservedRemoteResearchRuntime(IRemoteResearchClientRuntime runtime, SessionEvidenceLedger? evidence = null) => new EvidenceObservingRemoteResearchClientRuntime(runtime, evidence);

    public async Task<BrowserProductRuntime> GetBrowserProductAsync(CancellationToken cancellationToken = default)
    {
        await using var lease = await _lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
        var host = await GetBrowserHostUnderLeaseAsync(cancellationToken).ConfigureAwait(false);
        if (_browserProduct is not null) return _browserProduct;
        var product = host.CreateProductRuntime();
        product.BindCompositionLifetime(_lifetime);
        _browserProduct = product;
        return product;
    }

    private async Task<BrowserHostRuntime> GetBrowserHostUnderLeaseAsync(CancellationToken cancellationToken)
    {
        if (_browser is not null) return _browser;
        var browserDirectory = Path.Combine(_stateDirectory, "browser");
        var browser = await BrowserHostRuntime.CreateAsync(browserDirectory, BrowserOptionsFromEnvironment(), cancellationToken).ConfigureAwait(false);
        _browser = browser;
        return browser;
    }

    private async Task<ICrashConsistentBrowserGoalHost> GetBrowserGoalHostUnderLeaseAsync(CancellationToken cancellationToken)
    {
        // The optional factory is an internal composition seam only. Production construction
        // never supplies it, so the normal path still owns and lazily starts BrowserHostRuntime.
        // Keeping the seam at the least-authority goal-host contract lets deterministic tests
        // qualify root lifetime behavior without granting access to Playwright or credentials.
        if (_browserGoalHostFactory is not null)
            return await _browserGoalHostFactory(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Browser goal host factory returned null.");

        var browser = await GetBrowserHostUnderLeaseAsync(cancellationToken).ConfigureAwait(false);
        return new BrowserHostGoalHostAdapter(browser);
    }

    public async Task<IBrowserGoalAgent> CreateBrowserGoalAgentAsync(CancellationToken cancellationToken = default)
    {
        await using var lease = await _lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
        var browserGoalHost = await GetBrowserGoalHostUnderLeaseAsync(cancellationToken).ConfigureAwait(false);
        var observedHost = CreateObservedBrowserGoalHost(browserGoalHost);
        var inner = new BrowserGoalAgent(observedHost, new NemotronBrowserPlanner(_inference), _browserGoalStore);
        var agent = new LifetimeBoundBrowserGoalAgent(inner);
        agent.BindCompositionLifetime(_lifetime);
        return agent;
    }

    internal static ICrashConsistentBrowserGoalHost CreateObservedBrowserGoalHost(ICrashConsistentBrowserGoalHost host) => new EvidenceObservingBrowserGoalHost(host);

    public async Task<IReadOnlyList<BrowserGoalSession>> ListBrowserGoalSessionsAsync(CancellationToken cancellationToken = default)
    {
        await using var lease = await _lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
        return await _browserGoalStore.ListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<BrowserAmbiguousRecoveryService> CreateBrowserAmbiguousRecoveryServiceAsync(CancellationToken cancellationToken = default)
    {
        await using var lease = await _lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
        var browser = await GetBrowserHostUnderLeaseAsync(cancellationToken).ConfigureAwait(false);
        var recovery = new BrowserAmbiguousRecoveryService(browser, _browserGoalStore);
        recovery.BindCompositionLifetime(_lifetime);
        return recovery;
    }

    public async ValueTask DisposeAsync()
    {
        var disposalLease = await _lifetime.BeginDisposeAsync().ConfigureAwait(false);
        if (disposalLease is null) return;
        await using (disposalLease)
        {
            if (_browser is not null) await _browser.DisposeAsync().ConfigureAwait(false);
            _browser = null;
            _browserProduct = null;
            Session.Dispose();
            _memory.Dispose();
            _memoryEmbeddingProvider?.Dispose();
            _memoryStore.Dispose();
            _researchServerlessHttp?.Dispose();
            _researchObjectStorage?.Dispose();
            _tavilyHttp?.Dispose();
            _nebiusHttp.Dispose();
        }
    }

    private static BrowserHostOptions BrowserOptionsFromEnvironment()
    {
        var startRaw = Environment.GetEnvironmentVariable("NVIDEA_BROWSER_START_URL");
        var start = string.IsNullOrWhiteSpace(startRaw) ? BrowserHostOptions.Default.StartUri : Uri.TryCreate(startRaw.Trim(), UriKind.Absolute, out var parsed) ? parsed : throw new InvalidOperationException("NVIDEA_BROWSER_START_URL must be an absolute HTTP(S) URL.");
        var allowRaw = Environment.GetEnvironmentVariable("NVIDEA_BROWSER_ALLOWED_HOSTS");
        IReadOnlySet<string>? allowedHosts = null;
        if (!string.IsNullOrWhiteSpace(allowRaw))
            allowedHosts = allowRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(static x => x.ToLowerInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var headless = bool.TryParse(Environment.GetEnvironmentVariable("NVIDEA_BROWSER_HEADLESS"), out var parsedHeadless) && parsedHeadless;
        return new BrowserHostOptions(start, allowedHosts, headless);
    }

    private static string ResolveStateDirectory(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested)) return Path.GetFullPath(requested);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData)) throw new InvalidOperationException("Local application data directory is unavailable.");
        return Path.Combine(localAppData, "NVIDEA");
    }

    private sealed class BrowserHostGoalHostAdapter : ICrashConsistentBrowserGoalHost
    {
        private readonly BrowserHostRuntime _host;
        public BrowserHostGoalHostAdapter(BrowserHostRuntime host) => _host = host ?? throw new ArgumentNullException(nameof(host));
        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default) => _host.ObserveAsync(cancellationToken);
        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default) => _host.StartActionAsync(action, cancellationToken);
        public Task<BrowserJobOutcome> CreateActionAsync(Guid jobId, BrowserAction action, CancellationToken cancellationToken = default) => _host.CreateActionAsync(jobId, action, cancellationToken);
        public Task<BrowserJobOutcome> AdvanceActionAsync(Guid jobId, CancellationToken cancellationToken = default) => _host.AdvanceActionAsync(jobId, cancellationToken);
        public Task<BrowserJobOutcome?> GetAsync(Guid jobId, CancellationToken cancellationToken = default) => _host.GetAsync(jobId, cancellationToken);
        public Task<BrowserJobOutcome> RearmApprovalAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default) => _host.RearmApprovalAsync(jobId, exactScope, cancellationToken);
        public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default) => _host.ApproveAndResumeAsync(jobId, exactScope, cancellationToken);
        public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default) => _host.CancelAsync(jobId, cancellationToken);
    }
}
