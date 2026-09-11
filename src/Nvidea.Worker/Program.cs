using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;
using Nvidea.Core.Research;

namespace Nvidea.Worker;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            var options = WorkerCommandLine.Parse(args);
            using var http = CreateProviderHttpClient();

            var inference = new NebiusTokenFactoryClient(http, NebiusOptions.FromEnvironment());
            var tavily = new TavilyResearchClient(http, TavilyOptions.FromEnvironment());
            var engine = new ResearchEngine(inference, tavily);
            var handler = new ResearchJobHandler(engine);
            var transport = new DirectoryProtectedResearchTransport(GetRequiredEnvironment("NVIDEA_TRANSPORT_ROOT"));
            var clientPublicKey = GetRequiredEnvironment("NVIDEA_CLIENT_PUBLIC_KEY_PEM");
            var bindingWaiter = new ResearchDispatchBindingWaiter(
                transport,
                clientPublicKey,
                pollInterval: GetOptionalDurationSeconds("NVIDEA_BINDING_POLL_SECONDS", TimeSpan.FromSeconds(2)),
                maxWait: GetOptionalDurationSeconds("NVIDEA_BINDING_WAIT_SECONDS", TimeSpan.FromMinutes(5)));
            var binding = await bindingWaiter.WaitAsync(options.OpaqueWorkItemId, CancellationToken.None).ConfigureAwait(false);

            var worker = new NebiusResearchWorker(
                transport,
                transport,
                handler,
                GetRequiredEnvironment("NVIDEA_WORKER_PRIVATE_KEY_PEM"),
                clientPublicKey);

            await worker.ExecuteOneStageAsync(
                options.OpaqueWorkItemId,
                binding.RemoteJobId,
                CancellationToken.None).ConfigureAwait(false);

            Console.WriteLine("nvidea_worker_completed stage=research");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"nvidea_worker_failed error_type={ex.GetType().Name}");
            return 1;
        }
    }

    /// <summary>
    /// Provider requests can contain bearer credentials, Tavily API keys, prompts and research
    /// evidence. Automatic redirects are disabled so those values cannot be replayed to a different
    /// origin if a provider or intermediary returns a redirect.
    /// </summary>
    internal static HttpClient CreateProviderHttpClient()
    {
        return new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false
        }, disposeHandler: true);
    }

    private static string GetRequiredEnvironment(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Required worker environment variable '{name}' is missing.")
            : value;
    }

    private static TimeSpan GetOptionalDurationSeconds(string name, TimeSpan fallback)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;
        if (!double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds)
            || !double.IsFinite(seconds)
            || seconds <= 0)
        {
            throw new InvalidOperationException($"Worker environment variable '{name}' must be a positive number of seconds.");
        }
        return TimeSpan.FromSeconds(seconds);
    }
}

internal sealed record WorkerCommandLine(string OpaqueWorkItemId)
{
    public static WorkerCommandLine Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Count != 2 || !string.Equals(args[0], "research", StringComparison.Ordinal))
            throw new ArgumentException("Expected: research --work-item-id=<opaque-id>");

        const string prefix = "--work-item-id=";
        if (!args[1].StartsWith(prefix, StringComparison.Ordinal))
            throw new ArgumentException("A single opaque work-item id is required.");

        var opaqueId = args[1][prefix.Length..];
        if (opaqueId.Length < 24
            || opaqueId.Length > 64
            || opaqueId.Any(static ch => !(char.IsLetterOrDigit(ch) || ch is '-' or '_')))
        {
            throw new ArgumentException("The opaque work-item id is invalid.");
        }

        return new WorkerCommandLine(opaqueId);
    }
}
