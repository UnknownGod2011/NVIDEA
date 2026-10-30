using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.WorkerSigningCanaryVerifier;

internal static class Program
{
    private const string EvidencePathEnvironmentVariable = "NVIDEA_WORKER_SIGNING_CANARY_EVIDENCE_PATH";

    public static async Task<int> Main()
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                throw new InvalidOperationException();

            var path = Environment.GetEnvironmentVariable(EvidencePathEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) || !File.Exists(path))
                throw new InvalidOperationException();

            // This is intentionally configuration-only: it creates no provider client and makes no network call.
            var configuration = NebiusResearchLiveConfigurationLoader.LoadFromEnvironment();
            var pin = WorkerResultVerificationPublicKeyTrust.LoadRequired();
            var deployment = WorkerSigningCanaryDeployment.Create(configuration.DispatchOptions, pin);
            var store = new WorkerSigningCanaryEvidenceStore(path, new WindowsDpapiLocalStateProtector());
            var receipt = await store.LoadAsync().ConfigureAwait(false);
            var evidence = WorkerSigningCanaryEvidenceEvaluator.Evaluate(receipt, deployment, pin);
            if (evidence.Status != WorkerSigningCanaryStatus.Verified)
                throw new InvalidOperationException();

            Console.WriteLine("NVIDEA worker signing canary: VERIFIED");
            return 0;
        }
        catch
        {
            // Never disclose paths, deployment identifiers, protected receipt content, or provider diagnostics.
            Console.Error.WriteLine("NVIDEA worker signing canary: NOT VERIFIED");
            return 1;
        }
    }
}
