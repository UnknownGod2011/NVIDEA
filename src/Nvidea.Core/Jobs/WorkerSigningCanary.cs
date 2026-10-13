using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nvidea.Core.Jobs;

/// <summary>
/// A payload-free, domain-separated worker-key possession challenge. This proves that the signer
/// possessed the private key at challenge time; only a trusted, live Serverless response channel
/// can additionally establish that the deployed Nebius worker was the signer.
/// </summary>
public static class WorkerSigningCanary
{
    public const string Protocol = "nvidea.worker.signing-canary.v1";
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromSeconds(30);
    private const int NonceBytes = 32;

    public static WorkerSigningCanaryChallenge Create(DateTimeOffset? now = null)
    {
        var issuedAt = now ?? DateTimeOffset.UtcNow;
        return new(Protocol, ToBase64Url(RandomNumberGenerator.GetBytes(NonceBytes)),
            issuedAt, issuedAt.Add(Lifetime));
    }

    /// <summary>A bounded public argument for Serverless command dispatch; it contains no secrets.</summary>
    public static string EncodeChallenge(WorkerSigningCanaryChallenge challenge)
    {
        Validate(challenge, DateTimeOffset.UtcNow);
        return ToBase64Url(JsonSerializer.SerializeToUtf8Bytes(challenge));
    }

    public static WorkerSigningCanaryChallenge DecodeChallenge(string encoded, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(encoded) || encoded.Length > 2048)
            throw new CryptographicException("Signing canary challenge encoding is invalid.");
        try
        {
            var bytes = FromBase64Url(encoded);
            var challenge = JsonSerializer.Deserialize<WorkerSigningCanaryChallenge>(bytes)
                ?? throw new CryptographicException("Signing canary challenge is empty.");
            Validate(challenge, now ?? DateTimeOffset.UtcNow);
            return challenge;
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException)
        {
            throw new CryptographicException("Signing canary challenge is malformed.", ex);
        }
    }

    public static WorkerSigningCanaryProof Sign(
        WorkerSigningCanaryChallenge challenge,
        string signingPrivateKeyPem,
        DateTimeOffset? now = null)
    {
        Validate(challenge, now ?? DateTimeOffset.UtcNow);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(WorkerResultSigningPrivateKeyTrust.ValidateAndCanonicalize(signingPrivateKeyPem));
        var signature = rsa.SignData(Commitment(challenge), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return new(Protocol, challenge.Nonce, challenge.IssuedAt, challenge.ExpiresAt,
            Convert.ToBase64String(signature));
    }

    public static WorkerSigningCanaryVerification Verify(
        WorkerSigningCanaryChallenge expectedChallenge,
        WorkerSigningCanaryProof proof,
        string pinnedWorkerPublicKeyPem,
        DateTimeOffset? now = null)
    {
        var verifiedAt = now ?? DateTimeOffset.UtcNow;
        Validate(expectedChallenge, verifiedAt);
        ArgumentNullException.ThrowIfNull(proof);
        if (!string.Equals(proof.Protocol, expectedChallenge.Protocol, StringComparison.Ordinal)
            || !string.Equals(proof.Nonce, expectedChallenge.Nonce, StringComparison.Ordinal)
            || proof.IssuedAt != expectedChallenge.IssuedAt
            || proof.ExpiresAt != expectedChallenge.ExpiresAt)
            throw new CryptographicException("Signing canary proof does not match the outstanding challenge.");

        if (string.IsNullOrWhiteSpace(proof.SignatureBase64) || proof.SignatureBase64.Length > 1024)
            throw new CryptographicException("Signing canary signature is missing or oversized.");
        byte[] signature;
        try { signature = Convert.FromBase64String(proof.SignatureBase64); }
        catch (FormatException ex) { throw new CryptographicException("Signing canary signature is malformed.", ex); }

        var canonicalPublicKey = WorkerResultVerificationPublicKeyTrust.ValidateAndCanonicalize(pinnedWorkerPublicKeyPem);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(canonicalPublicKey);
        if (!rsa.VerifyData(Commitment(expectedChallenge), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss))
            throw new CryptographicException("Signing canary signature does not match the pinned worker identity.");

        return new(WorkerResultVerificationPublicKeyTrust.GetSha256Fingerprint(canonicalPublicKey), verifiedAt);
    }

    private static void Validate(WorkerSigningCanaryChallenge challenge, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        if (!string.Equals(challenge.Protocol, Protocol, StringComparison.Ordinal))
            throw new CryptographicException("Unsupported signing canary protocol.");
        if (string.IsNullOrWhiteSpace(challenge.Nonce) || challenge.Nonce.Length != 43)
            throw new CryptographicException("Signing canary nonce has invalid length.");
        try
        {
            var nonce = FromBase64Url(challenge.Nonce);
            if (nonce.Length != NonceBytes || !string.Equals(ToBase64Url(nonce), challenge.Nonce, StringComparison.Ordinal))
                throw new CryptographicException("Signing canary nonce is not canonical.");
        }
        catch (FormatException ex) { throw new CryptographicException("Signing canary nonce is malformed.", ex); }

        if (challenge.ExpiresAt <= challenge.IssuedAt
            || challenge.ExpiresAt - challenge.IssuedAt > Lifetime
            || challenge.IssuedAt > now.Add(AllowedClockSkew)
            || challenge.ExpiresAt <= now)
            throw new CryptographicException("Signing canary challenge is expired, premature, or has an invalid lifetime.");
    }

    private static byte[] Commitment(WorkerSigningCanaryChallenge challenge) =>
        Encoding.UTF8.GetBytes(string.Join('\n', Protocol, challenge.Protocol, challenge.Nonce,
            challenge.IssuedAt.ToUniversalTime().ToString("O"),
            challenge.ExpiresAt.ToUniversalTime().ToString("O")));

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string encoded)
    {
        if (encoded.Any(c => !(c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_')))
            throw new FormatException("Invalid base64url character.");
        var padding = (4 - encoded.Length % 4) % 4;
        if (padding == 3) throw new FormatException("Invalid base64url length.");
        return Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/') + new string('=', padding));
    }
}

public sealed record WorkerSigningCanaryChallenge(
    string Protocol, string Nonce, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt);

public sealed record WorkerSigningCanaryProof(
    string Protocol, string Nonce, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt, string SignatureBase64);

/// <summary>Public-only cryptographic evidence. This does not attest to a Nebius deployment by itself.</summary>
public sealed record WorkerSigningCanaryVerification(string VerificationKeySha256, DateTimeOffset VerifiedAt);

/// <summary>
/// Prevents accepting the same challenge twice in one verification session. A fresh random challenge
/// must be created for every remote probe; do not persist/reuse challenges as recording evidence.
/// </summary>
public sealed class WorkerSigningCanaryVerifier
{
    private readonly WorkerSigningCanaryChallenge _challenge;
    private int _consumed;

    public WorkerSigningCanaryVerifier(WorkerSigningCanaryChallenge challenge) =>
        _challenge = challenge ?? throw new ArgumentNullException(nameof(challenge));

    public WorkerSigningCanaryVerification VerifyAndConsume(
        WorkerSigningCanaryProof proof, string pinnedWorkerPublicKeyPem, DateTimeOffset? now = null)
    {
        var verification = WorkerSigningCanary.Verify(_challenge, proof, pinnedWorkerPublicKeyPem, now);
        if (Interlocked.CompareExchange(ref _consumed, 1, 0) != 0)
            throw new CryptographicException("Signing canary challenge was already consumed.");
        return verification;
    }
}
