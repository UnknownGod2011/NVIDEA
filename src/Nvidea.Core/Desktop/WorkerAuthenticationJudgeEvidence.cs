using Nvidea.Core.Jobs;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Public-only judge projection of the worker identity trusted to authenticate remote research results.
/// It structurally excludes PEM, deployment secret references, credentials, and research payloads.
/// </summary>
public sealed record WorkerAuthenticationJudgeEvidence(
    string VerificationKeySha256,
    string SignatureScheme,
    WorkerSigningCanaryStatus CanaryStatus = WorkerSigningCanaryStatus.PublicKeyPinnedCanaryNotRun,
    DateTimeOffset? CanaryVerifiedAt = null,
    string? DeploymentFingerprintSha256 = null)
{
    public const string RsaPssSha256 = "RSA-PSS/SHA-256";

    public static WorkerAuthenticationJudgeEvidence FromPinnedEnvironment() =>
        FromFingerprint(WorkerResultVerificationPublicKeyTrust.LoadRequiredSha256Fingerprint());

    public static WorkerAuthenticationJudgeEvidence FromFingerprint(string fingerprint)
    {
        if (fingerprint is null ||
            fingerprint.Length != 64 ||
            fingerprint.Any(character => !((character >= '0' && character <= '9') || (character >= 'A' && character <= 'F'))))
        {
            throw new InvalidOperationException("Worker verification fingerprint must be canonical SHA-256 hexadecimal.");
        }

        return new WorkerAuthenticationJudgeEvidence(fingerprint, RsaPssSha256);
    }

    public static WorkerAuthenticationJudgeEvidence FromCanarySnapshot(WorkerSigningCanaryEvidenceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Status == WorkerSigningCanaryStatus.NotConfigured)
            throw new InvalidOperationException("A configured worker public verification identity is required.");
        var evidence = FromFingerprint(snapshot.VerificationKeySha256);
        return evidence with
        {
            CanaryStatus = snapshot.Status,
            CanaryVerifiedAt = snapshot.VerifiedAt,
            DeploymentFingerprintSha256 = snapshot.DeploymentFingerprintSha256
        };
    }

    public string ToStatusText() =>
        $"Worker result authentication: {FormatStatus(CanaryStatus)}\n" +
        $"Scheme: {SignatureScheme}\n" +
        $"Public verification fingerprint (SHA-256): {VerificationKeySha256}" +
        (CanaryVerifiedAt is { } verifiedAt ? $"\nCanary verified at (UTC): {verifiedAt:O}" : string.Empty) +
        (string.IsNullOrWhiteSpace(DeploymentFingerprintSha256) ? string.Empty : $"\nDeployment fingerprint (SHA-256): {DeploymentFingerprintSha256}");

    private static string FormatStatus(WorkerSigningCanaryStatus status) => status switch
    {
        WorkerSigningCanaryStatus.PublicKeyPinnedCanaryNotRun => "PUBLIC KEY PINNED — CANARY NOT RUN",
        WorkerSigningCanaryStatus.Running => "CANARY RUNNING",
        WorkerSigningCanaryStatus.Failed => "CANARY FAILED",
        WorkerSigningCanaryStatus.Verified => "CANARY VERIFIED",
        WorkerSigningCanaryStatus.Stale => "CANARY STALE — DEPLOYMENT OR PUBLIC PIN CHANGED",
        _ => "NOT CONFIGURED"
    };
}
