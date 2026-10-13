using Nvidea.Core.Jobs;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Public-only judge projection of the worker identity trusted to authenticate remote research results.
/// It structurally excludes PEM, deployment secret references, credentials, and research payloads.
/// </summary>
public sealed record WorkerAuthenticationJudgeEvidence(
    string VerificationKeySha256,
    string SignatureScheme)
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

    public string ToStatusText() =>
        $"Worker result authentication: PINNED\nScheme: {SignatureScheme}\nPublic verification fingerprint (SHA-256): {VerificationKeySha256}";
}
