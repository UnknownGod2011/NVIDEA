using System.Security.Cryptography;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Single trust boundary for the client RSA public identity used to wrap protected remote-research
/// result data keys. The key must be bounded, public-only RSA material of at least 2048 bits and
/// must prove the exact OAEP-SHA256 encryption capability used by the result protocol.
/// </summary>
public static class ClientResultEnvelopePublicKeyTrust
{
    private const int MaxPemCharacters = 65536;
    private const int MinimumRsaBits = 2048;
    private const int CapabilityProbeBytes = 32;

    public static string ValidateAndCanonicalize(string pem)
    {
        using var rsa = CreateValidatedRsa(pem);
        return rsa.ExportSubjectPublicKeyInfoPem();
    }

    public static RSA CreateValidatedRsa(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem)
            || pem.Length > MaxPemCharacters
            || pem.Any(static character => char.IsControl(character) && character is not '\r' and not '\n'))
        {
            throw new InvalidOperationException("The client result-envelope public key is missing or invalid.");
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
                    "The client result-envelope public key is not valid RSA public-key PEM.",
                    exception);
            }

            if (rsa.KeySize < MinimumRsaBits)
                throw new InvalidOperationException("The client result-envelope RSA public key must be at least 2048 bits.");

            RejectPrivateMaterial(rsa);
            ProvePublicOaepSha256Capability(rsa);
            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    private static void RejectPrivateMaterial(RSA rsa)
    {
        try
        {
            _ = rsa.ExportParameters(includePrivateParameters: true);
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            return;
        }

        throw new InvalidOperationException(
            "The client result-envelope encryption key must be public-only RSA material; private key material is not permitted.");
    }

    private static void ProvePublicOaepSha256Capability(RSA rsa)
    {
        byte[]? probe = null;
        byte[]? wrapped = null;

        try
        {
            probe = RandomNumberGenerator.GetBytes(CapabilityProbeBytes);
            wrapped = rsa.Encrypt(probe, RSAEncryptionPadding.OaepSHA256);
            if (wrapped.Length == 0)
            {
                throw new InvalidOperationException(
                    "The client result-envelope RSA public key failed the OAEP-SHA256 capability proof.");
            }
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            throw new InvalidOperationException(
                "The client result-envelope key must contain usable RSA public material for OAEP-SHA256 encryption.",
                exception);
        }
        finally
        {
            if (probe is not null)
                CryptographicOperations.ZeroMemory(probe);
            if (wrapped is not null)
                CryptographicOperations.ZeroMemory(wrapped);
        }
    }
}
