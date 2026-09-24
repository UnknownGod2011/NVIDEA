using System.Runtime.InteropServices;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;
using Nvidea.Core.Research;

namespace Nvidea.Worker;

internal static class Program
{
    private static readonly TimeSpan SigTermGracePeriod = TimeSpan.FromSeconds(20);

    private static async Task<int> Main(string[] args)
    {
        using var shutdown = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            shutdown.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;
        using var sigTermRegistration = RegisterSigTerm(shutdown);

        try
        {
            var cancellationToken = shutdown.Token;
            var options = WorkerCommandLine.Parse(args);

            // Load the entire runtime through one trust-ordered environment boundary. Public
            // bootstrap/destination/timing validation completes before provider or worker secrets
            // are accessed, and the same path is directly regression-testable in Core.
            var runtime = NebiusResearchWorkerRuntimeConfiguration.Load();

            // Result authentication is mandatory for the deployed worker. This is a distinct key
            // role from work-item decryption: a worker must never publish an unsigned result merely
            // because result-signing secret provisioning was omitted or malformed.
            var workerResultSigningPrivateKey = WorkerResultSigningPrivateKeyTrust.LoadRequired();

            using var http = CreateProviderHttpClient();
            var inference = new NebiusTokenFactoryClient(http, runtime.Nebius);
            var tavily = new TavilyResearchClient(http, runtime.Tavily);
            var engine = new ResearchEngine(inference, tavily);
            var handler = new ResearchJobHandler(engine);
            var transport = new DirectoryProtectedResearchTransport(runtime.BootstrapTrust.TransportRoot);
            var clientVerificationPublicKey = runtime.BootstrapTrust.ClientVerificationPublicKeyPem;
            var clientResultEncryptionPublicKey = runtime.BootstrapTrust.ClientResultEncryptionPublicKeyPem;

            // Mounted Object Storage can expose a short propagation/reconnect window before the
            // encrypted work item is readable. Retry only absence and transport I/O under a bounded,
            // cancellation-aware bootstrap budget; malformed or substituted envelopes fail closed.
            var workItemTransport = (IProtectedResearchWorkItemTransport)transport;
            var workItemLoader = new WorkerProtectedResearchWorkItemLoader(
                workItemTransport,
                pollInterval: runtime.WorkItemPollInterval,
                maxWait: runtime.WorkItemMaxWait);
            var stagedWorkItem = await workItemLoader
                .LoadAsync(options.OpaqueWorkItemId, cancellationToken)
                .ConfigureAwait(false);

            // The V2 signed binding commits to the exact staged encrypted envelope. This comparison
            // occurs before payload decryption or provider/model execution, so a writable shared mount
            // cannot substitute a different validly encrypted envelope for the same opaque id.
            var bindingWaiter = new ResearchDispatchBindingWaiter(
                transport,
                clientVerificationPublicKey,
                pollInterval: runtime.BindingPollInterval,
                maxWait: runtime.BindingMaxWait);
            var binding = await bindingWaiter
                .WaitAsync(options.OpaqueWorkItemId, stagedWorkItem, cancellationToken)
                .ConfigureAwait(false);

            // Pin the already-verified envelope in memory for the execution primitive. The legacy
            // worker API performs a work-item transport read internally; feeding it this one-object
            // transport removes the verify-then-re-read TOCTOU against the writable shared mount.
            var pinnedWorkItem = new PinnedResearchWorkItemTransport(stagedWorkItem);
            var worker = new NebiusResearchWorker(
                pinnedWorkItem,
                transport,
                handler,
                runtime.WorkerPrivateKeyPem,
                clientResultEncryptionPublicKey,
                workerResultSigningPrivateKey);

            await worker.ExecuteOneStageAsync(
                options.OpaqueWorkItemId,
                binding.RemoteJobId,
                cancellationToken).ConfigureAwait(false);

            Console.WriteLine("nvidea_worker_completed stage=research");
            return 0;
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
            Console.Error.WriteLine("nvidea_worker_cancelled reason=process_shutdown");
            return 130;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"nvidea_worker_failed error_type={ex.GetType().Name}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    internal static HttpClient CreateProviderHttpClient()
    {
        return new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false
        }, disposeHandler: true);
    }

    private static IDisposable? RegisterSigTerm(CancellationTokenSource shutdown)
    {
        ArgumentNullException.ThrowIfNull(shutdown);
        if (!(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD()))
            return null;

        var watchdogCancellation = new CancellationTokenSource();
        var requested = 0;
        var registration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
        {
            context.Cancel = true;
            if (Interlocked.Exchange(ref requested, 1) != 0)
            {
                Environment.Exit(143);
                return;
            }

            shutdown.Cancel();
            _ = ForceExitAfterGraceAsync(watchdogCancellation.Token);
        });

        return new CompositeDisposable(registration, watchdogCancellation);
    }

    private static async Task ForceExitAfterGraceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(SigTermGracePeriod, cancellationToken).ConfigureAwait(false);
            Environment.Exit(143);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private sealed class CompositeDisposable : IDisposable
    {
        private readonly IDisposable _registration;
        private readonly CancellationTokenSource _watchdogCancellation;
        private int _disposed;

        public CompositeDisposable(PosixSignalRegistration registration, CancellationTokenSource watchdogCancellation)
        {
            _registration = registration;
            _watchdogCancellation = watchdogCancellation;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            _watchdogCancellation.Cancel();
            _registration.Dispose();
            _watchdogCancellation.Dispose();
        }
    }

    /// <summary>
    /// Single-object immutable worker transport used only after the shared-mount envelope has passed
    /// the client-signed V2 commitment check. It intentionally provides no mutation surface.
    /// </summary>
    private sealed class PinnedResearchWorkItemTransport : IProtectedResearchWorkItemTransport
    {
        private readonly ProtectedResearchWorkItemEnvelope _envelope;

        public PinnedResearchWorkItemTransport(ProtectedResearchWorkItemEnvelope envelope)
        {
            _envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
        }

        public Task PutAsync(
            ProtectedResearchWorkItemEnvelope envelope,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Pinned worker transport is read-only.");

        public Task<ProtectedResearchWorkItemEnvelope?> GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<ProtectedResearchWorkItemEnvelope?>(
                string.Equals(opaqueWorkItemId, _envelope.OpaqueWorkItemId, StringComparison.Ordinal)
                    ? _envelope
                    : null);
        }

        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Pinned worker transport is read-only.");
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
