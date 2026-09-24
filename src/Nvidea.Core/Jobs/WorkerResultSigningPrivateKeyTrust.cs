using System.Security.Cryptography;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Trust boundary for the worker-only remote-result signing identity. The private key is loaded from
/// a dedicated secret environment variable and must never be reused as the work-item decryption key.
/// </summary>
public static class WorkerResultSigningPrivateKeyTrust
{
    public const string EnvironmentVariable = "NVIDEA_WORKER_RESULT_SIGNING_PRIVATE_KEY_PEM";

    public static string LoadRequired(Func<string, string?>? environmentReader = null)
    {
        var read = environmentReader ?? Environment.GetEnvironmentVariable;
        var pem = read(EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(pem))
            throw new InvalidOperationException($"Required worker secret '{EnvironmentVariable}' is missing.");

        return ValidateAndCanonicalize(pem);
    }

    public static string ValidateAndCanonicalize(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem))
            throw new ArgumentException("Worker result-signing private key is required.", nameof(pem));

        using var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(pem);
            var parameters = rsa.ExportParameters(true);
            if (rsa.KeySize < 2048 || parameters.D is null)
                throw new CryptographicException("Worker result-signing identity must be an RSA private key of at least 2048 bits.");

            return rsa.ExportRSAPrivateKeyPem();
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
            throw new CryptographicException("Worker result-signing private key is invalid.", ex);
        }
    }
}
