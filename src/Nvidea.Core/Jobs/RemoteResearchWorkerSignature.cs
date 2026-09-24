using System.Security.Cryptography;
using System.Text;

namespace Nvidea.Core.Jobs;

/// <summary>Canonical worker-origin signature primitive for the v2 remote-result protocol. The commitment covers every encrypted-envelope field that carries result/provenance authority. It intentionally excludes no mutable transport field.</summary>
public static class RemoteResearchWorkerSignature
{
    public const string Domain = "nvidea.research.remote-result.worker-signature.v1";

    public static string Sign(ProtectedResearchResultEnvelope envelope, string workerPrivateKeyPem)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        using var rsa = ImportPrivate(workerPrivateKeyPem);
        return Convert.ToBase64String(rsa.SignData(Commitment(envelope), HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
    }

    public static void Verify(ProtectedResearchResultEnvelope envelope, string signatureBase64, string pinnedWorkerPublicKeyPem)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (string.IsNullOrWhiteSpace(signatureBase64)) throw new CryptographicException("Remote result worker signature is missing.");
        byte[] signature;
        try { signature = Convert.FromBase64String(signatureBase64); }
        catch (FormatException ex) { throw new CryptographicException("Remote result worker signature is malformed.", ex); }
        using var rsa = ImportPublic(pinnedWorkerPublicKeyPem);
        if (!rsa.VerifyData(Commitment(envelope), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss))
            throw new CryptographicException("Remote result worker signature is invalid.");
    }

    private static byte[] Commitment(ProtectedResearchResultEnvelope e) => Encoding.UTF8.GetBytes(string.Join('\n', Domain, e.ProtocolVersion, e.OpaqueWorkItemId, e.RemoteJobId, e.WrappedDataKey, e.Nonce, e.Ciphertext, e.AuthenticationTag, e.CompletedAt.ToUniversalTime().ToString("O"), e.ExpiresAt.ToUniversalTime().ToString("O")));

    private static RSA ImportPrivate(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem)) throw new ArgumentException("Worker signing private key is required.", nameof(pem));
        var rsa = RSA.Create();
        try { rsa.ImportFromPem(pem); if (rsa.KeySize < 2048 || rsa.ExportParameters(true).D is null) throw new CryptographicException("Worker signing identity must be an RSA private key of at least 2048 bits."); return rsa; }
        catch { rsa.Dispose(); throw; }
    }

    private static RSA ImportPublic(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem)) throw new ArgumentException("Pinned worker verification key is required.", nameof(pem));
        var rsa = RSA.Create();
        try { rsa.ImportFromPem(pem); if (rsa.KeySize < 2048) throw new CryptographicException("Worker verification identity must be an RSA public key of at least 2048 bits."); return rsa; }
        catch { rsa.Dispose(); throw; }
    }
}

/// <summary>
/// Narrow authenticated result boundary for the v2 migration. Worker identity is verified over the
/// still-encrypted envelope before client-key decryption is attempted, so unauthenticated remote data
/// never reaches JSON parsing or result provenance validation. The pinned worker key is supplied by
/// trusted client configuration and is never selected from the remote result.
/// </summary>
public static class AuthenticatedResearchResultProtector
{
    public static RemoteResearchStageResult Unprotect(
        ProtectedResearchResultEnvelope envelope,
        string workerSignatureBase64,
        string pinnedWorkerPublicKeyPem,
        string clientPrivateKeyPem,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        RemoteResearchWorkerSignature.Verify(envelope, workerSignatureBase64, pinnedWorkerPublicKeyPem);
        return ResearchResultProtector.Unprotect(envelope, clientPrivateKeyPem, now);
    }
}
