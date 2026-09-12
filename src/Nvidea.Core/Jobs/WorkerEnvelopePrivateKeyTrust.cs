using System.Security.Cryptography;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Single trust boundary for the worker RSA private identity used to unwrap protected research
/// work-item data keys. The key must be bounded private RSA material of at least 2048 bits and must
/// prove the exact OAEP-SHA256 capability used by the remote research protocol.
/// </summary>
public static class WorkerEnvelopePrivateKeyTrust
{
    private const int MaxPemCharacters = 65536;
    private const int MinimumRsaBits = 2048;
    private const int CapabilityProbeBytes = 32;

    public static string ValidateAndCanonicalize(string pem)
    {
        using var rsa = CreateValidatedRsa(pem);
        return rsa.ExportPkcs8PrivateKeyPem();
    }

    public static RSA CreateValidatedRsa(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem)
            || pem.Length > MaxPemCharacters
            || pem.Any(static character => char.IsControl(character) && character is not '\r' and not '\n'))
        {
            throw new InvalidOperationException("The worker envelope private key is missing or invalid.");
        }

        var rsa = RSA.Create();
        try
        {
            try
            {
                rsa.ImportFromPem(pem);
            }
            catch (Exception exception) when (exception is CryptographicException or ArgumentException)
            {
                throw new InvalidOperationException(
                    "The worker envelope private key is not valid RSA private-key PEM.",
                    exception);
            }

            if (rsa.KeySize < MinimumRsaBits)
                throw new InvalidOperationException("The worker envelope RSA private key must be at least 2048 bits.");

            ProvePrivateOaepSha256Capability(rsa);
            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    private static void ProvePrivateOaepSha256Capability(RSA rsa)
    {
        byte[]? probe = null;
        byte[]? wrapped = null;
        byte[]? unwrapped = null;

        try
        {
            try
            {
                _ = rsa.ExportParameters(includePrivateParameters: true);
            }
            catch (Exception exception) when (exception is CryptographicException or ArgumentException)
            {
                throw new InvalidOperationException(
                    "The worker envelope key must contain usable RSA private material for OAEP-SHA256 decryption.",
                    exception);
            }

            probe = RandomNumberGenerator.GetBytes(CapabilityProbeBytes);
            wrapped = rsa.Encrypt(probe, RSAEncryptionPadding.OaepSHA256);
            unwrapped = rsa.Decrypt(wrapped, RSAEncryptionPadding.OaepSHA256);
            if (!CryptographicOperations.FixedTimeEquals(probe, unwrapped))
            {
                throw new InvalidOperationException(
                    "The worker envelope RSA private key failed the OAEP-SHA256 capability proof.");
            }
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            throw new InvalidOperationException(
                "The worker envelope key must contain usable RSA private material for OAEP-SHA256 decryption.",
                exception);
        }
        finally
        {
            if (probe is not null)
                CryptographicOperations.ZeroMemory(probe);
            if (wrapped is not null)
                CryptographicOperations.ZeroMemory(wrapped);
            if (unwrapped is not null)
                CryptographicOperations.ZeroMemory(unwrapped);
        }
    }
}
