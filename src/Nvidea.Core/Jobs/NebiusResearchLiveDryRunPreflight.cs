using System.Security.Cryptography;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Zero-cost validation for the exact live research deployment inputs. This performs no provider
/// calls and intentionally does not resolve MysteryBox secret values. It exists to reject malformed,
/// mutable, or cryptographically inconsistent deployment configuration before cloud resources are created.
/// </summary>
public static class NebiusResearchLiveDryRunPreflight
{
    public static void Validate(
        NebiusResearchDispatchOptions dispatchOptions,
        NebiusObjectStorageClientOptions objectStorageOptions,
        string serverlessAccessToken,
        string projectId,
        string clientPrivateKeyPem)
    {
        ArgumentNullException.ThrowIfNull(dispatchOptions);
        ArgumentNullException.ThrowIfNull(objectStorageOptions);

        NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment(dispatchOptions, objectStorageOptions);
        ValidateOpaqueCredential(serverlessAccessToken, nameof(serverlessAccessToken));
        ValidateBounded(projectId, nameof(projectId), 256);
        ValidateDispatchShape(dispatchOptions);
        ValidateObjectStorageShape(objectStorageOptions);
        ValidateSigningIdentity(dispatchOptions, clientPrivateKeyPem);
    }

    /// <summary>
    /// Validates the client dispatch-signing identity without touching any provider credentials.
    /// Returns the canonical public identity only after proving the PEM contains usable RSA private
    /// material with an acceptable key size and can perform the signature operation used by dispatch.
    /// </summary>
    internal static string ValidateAndDeriveClientPublicKey(string clientPrivateKeyPem)
    {
        ValidateBoundedPem(clientPrivateKeyPem, nameof(clientPrivateKeyPem), 65536);
        using var privateKey = RSA.Create();
        try
        {
            privateKey.ImportFromPem(clientPrivateKeyPem);
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException("Client dispatch-signing private key PEM is not a valid RSA private key.");
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("Client dispatch-signing private key PEM is not a valid RSA private key.");
        }

        if (privateKey.KeySize < 2048)
            throw new InvalidOperationException("Client dispatch-signing RSA key must be at least 2048 bits.");

        // ImportFromPem also accepts a public-only RSA PEM. Prove that private key material is
        // actually present with a harmless fixed-hash signature before allowing any later provider
        // credential reads or paid live execution.
        try
        {
            _ = privateKey.SignHash(
                SHA256.HashData(Array.Empty<byte>()),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException("Client dispatch-signing private key PEM does not contain usable RSA private key material.");
        }

        return privateKey.ExportSubjectPublicKeyInfoPem();
    }

    private static void ValidateDispatchShape(NebiusResearchDispatchOptions options)
    {
        ValidateBounded(options.WorkerImage, nameof(options.WorkerImage), 2048);
        ValidateBounded(options.ContainerCommand, nameof(options.ContainerCommand), 256);
        ValidateBounded(options.Platform, nameof(options.Platform), 256);
        ValidateBounded(options.Preset, nameof(options.Preset), 256);
        ValidateBounded(options.Timeout, nameof(options.Timeout), 128);
        ValidateBounded(options.SubnetId, nameof(options.SubnetId), 512);

        NebiusResearchDeploymentPreflight.ValidateDigestPinnedWorkerImage(options.WorkerImage);

        if (options.Disk is null
            || string.IsNullOrWhiteSpace(options.Disk.Type)
            || options.Disk.Type.Length > 128
            || options.Disk.Type.Any(char.IsControl)
            || options.Disk.SizeBytes <= 0)
        {
            throw new InvalidOperationException("Live Nebius research requires a bounded positive disk specification.");
        }

        NebiusResearchDeploymentPreflight.ValidateWorkerPublicKey(options.WorkerPublicKeyPem);
    }

    private static void ValidateObjectStorageShape(NebiusObjectStorageClientOptions options)
    {
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint)
            || !string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment))
        {
            throw new InvalidOperationException("Live Object Storage endpoint must be an absolute HTTPS origin without embedded credentials, query, or fragment.");
        }

        ValidateBounded(options.Region, nameof(options.Region), 64);
        ValidateBounded(options.Bucket, nameof(options.Bucket), 63);
        ValidateOpaqueCredential(options.AccessKeyId, nameof(options.AccessKeyId));
        ValidateOpaqueCredential(options.SecretAccessKey, nameof(options.SecretAccessKey));

        if (options.OperationTimeout is { } timeout
            && (timeout < TimeSpan.FromSeconds(1) || timeout > TimeSpan.FromMinutes(2)))
        {
            throw new InvalidOperationException("Live Object Storage operation timeout must be between 1 second and 2 minutes.");
        }
        if (options.MaxRetries is < 0 or > 5)
            throw new InvalidOperationException("Live Object Storage retries must be between 0 and 5.");
    }

    private static void ValidateSigningIdentity(NebiusResearchDispatchOptions options, string clientPrivateKeyPem)
    {
        var derivedPublicKeyPem = ValidateAndDeriveClientPublicKey(clientPrivateKeyPem);

        var plaintext = options.EnvironmentVariables ?? new Dictionary<string, string>();
        if (!plaintext.TryGetValue(NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable, out var configuredPublicKey)
            || string.IsNullOrWhiteSpace(configuredPublicKey))
        {
            throw new InvalidOperationException("The live dry-run requires the client verification public key in plaintext worker configuration.");
        }

        using var publicKey = RSA.Create();
        try
        {
            publicKey.ImportFromPem(configuredPublicKey);
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException("Configured client verification public key PEM is not a valid RSA public key.");
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("Configured client verification public key PEM is not a valid RSA public key.");
        }

        if (publicKey.KeySize < 2048)
            throw new InvalidOperationException("Client verification RSA key must be at least 2048 bits.");

        using var derivedPublicKey = RSA.Create();
        try
        {
            derivedPublicKey.ImportFromPem(derivedPublicKeyPem);
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            throw new InvalidOperationException("Derived client verification public key could not be canonicalized.", exception);
        }

        var derived = derivedPublicKey.ExportSubjectPublicKeyInfo();
        var configured = publicKey.ExportSubjectPublicKeyInfo();
        if (!CryptographicOperations.FixedTimeEquals(derived, configured))
        {
            throw new InvalidOperationException(
                "Client dispatch-signing private key does not match the public key configured for worker binding verification.");
        }
    }

    private static void ValidateOpaqueCredential(string value, string name)
    {
        ValidateBounded(value, name, 8192);
        if (value.Any(char.IsWhiteSpace))
            throw new InvalidOperationException($"Live configuration '{name}' must not contain whitespace.");
    }

    private static void ValidateBoundedPem(string value, string name, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > maximumLength
            || value.Any(static character => char.IsControl(character) && character is not '\r' and not '\n'))
        {
            throw new InvalidOperationException($"Live configuration '{name}' is missing or invalid.");
        }
    }

    private static void ValidateBounded(string value, string name, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > maximumLength
            || value.Any(char.IsControl))
        {
            throw new InvalidOperationException($"Live configuration '{name}' is missing or invalid.");
        }
    }
}
