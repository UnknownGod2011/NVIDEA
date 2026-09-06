using Nvidea.Core.Memory;
using Nvidea.Core.Nebius;
using Nvidea.Core.Research;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Owns the trusted provider graph for the desktop application. UI/plugin code receives
/// high-level services only; provider credentials and raw clients stay behind this root.
/// </summary>
public sealed class NvideaCompositionRoot : IAsyncDisposable
{
    private readonly HttpClient _nebiusHttp;
    private readonly HttpClient? _tavilyHttp;
    private readonly JsonFileMemoryStore _memoryStore;
    private readonly PersonalMemoryService _memory;

    private NvideaCompositionRoot(
        HttpClient nebiusHttp,
        HttpClient? tavilyHttp,
        JsonFileMemoryStore memoryStore,
        PersonalMemoryService memory,
        DesktopInvocationService desktop)
    {
        _nebiusHttp = nebiusHttp;
        _tavilyHttp = tavilyHttp;
        _memoryStore = memoryStore;
        _memory = memory;
        Desktop = desktop;
    }

    public DesktopInvocationService Desktop { get; }
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
        return new NvideaCompositionRoot(nebiusHttp, tavilyHttp, memoryStore, memory, desktop);
    }

    public async ValueTask DisposeAsync()
    {
        _memory.Dispose();
        _memoryStore.Dispose();
        _tavilyHttp?.Dispose();
        _nebiusHttp.Dispose();
        await ValueTask.CompletedTask;
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
