using System.Security.Cryptography;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Client-side trust boundary for the pinned remote-worker result-verification identity.
/// This configuration is intentionally public-key-only: the worker signing private key must
/// never be present in the desktop/client process or selected from an untrusted result envelope.
/// </summary>
public static class WorkerResultVerificationPublicKeyTrust
{
    public const string EnvironmentVariable = "NVIDEA_WORKER_RESULT_VERIFICATION_PUBLIC_KEY_PEM";

    public static string LoadRequired(Func<string, string?>? environmentReader = null)
    {
        var read = environmentReader ?? Environment.GetEnvironmentVariable;
        var pem = read(EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(pem))
            throw new InvalidOperationException($"Required client trust setting '{EnvironmentVariable}' is missing.");

        return ValidateAndCanonicalize(pem);
    }

    /// <summary>
    /// Returns the non-secret SHA-256 fingerprint of the canonical SubjectPublicKeyInfo bytes.
    /// This is the only worker-signing identity material suitable for judge/readiness evidence.
    /// </summary>
    public static string GetSha256Fingerprint(string pem)
    {
        var canonicalPem = ValidateAndCanonicalize(pem);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(canonicalPem);
        var subjectPublicKeyInfo = rsa.ExportSubjectPublicKeyInfo();
        return Convert.ToHexString(SHA256.HashData(subjectPublicKeyInfo));
    }

    public static string LoadRequiredSha256Fingerprint(Func<string, string?>? environmentReader = null) =>
        GetSha256Fingerprint(LoadRequired(environmentReader));

    public static string ValidateAndCanonicalize(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem))
            throw new ArgumentException("Worker result-verification public key is required.", nameof(pem));

        using var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(pem);
            if (rsa.KeySize < 2048)
                throw new CryptographicException("Worker result-verification identity must be an RSA public key of at least 2048 bits.");

            // Export only public parameters even when a caller accidentally supplies private material.
            // This guarantees the value retained by client composition cannot carry worker signing authority.
            return rsa.ExportSubjectPublicKeyInfoPem();
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (CryptographicException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Worker result-verification public key is invalid.", ex);
        }
    }
}
