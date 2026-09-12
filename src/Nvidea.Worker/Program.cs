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

            // Load the entire runtime through one trust-ordered environment boundary. Public
            // bootstrap/destination/timing validation completes before provider or worker secrets
            // are accessed, and the same path is directly regression-testable in Core.
            var runtime = NebiusResearchWorkerRuntimeConfiguration.Load();

            using var http = CreateProviderHttpClient();
            var inference = new NebiusTokenFactoryClient(http, runtime.Nebius);
            var tavily = new TavilyResearchClient(http, runtime.Tavily);
            var engine = new ResearchEngine(inference, tavily);
            var handler = new ResearchJobHandler(engine);
            var transport = new DirectoryProtectedResearchTransport(runtime.BootstrapTrust.TransportRoot);
            var clientPublicKey = runtime.BootstrapTrust.ClientVerificationPublicKeyPem;
            var bindingWaiter = new ResearchDispatchBindingWaiter(
                transport,
                clientPublicKey,
                pollInterval: runtime.BindingPollInterval,
                maxWait: runtime.BindingMaxWait);
            var binding = await bindingWaiter.WaitAsync(options.OpaqueWorkItemId, CancellationToken.None).ConfigureAwait(false);

            var worker = new NebiusResearchWorker(
                transport,
                transport,
                handler,
                runtime.WorkerPrivateKeyPem,
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
