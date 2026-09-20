using System.Text;
using System.Text.Json;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Browser;

/// <summary>
/// Protected, atomic persistence for the latest completed browser verification receipt.
/// The receipt is deliberately non-authorizing: this store never contains approval scopes/tokens,
/// URLs, locators, typed values, page content, verification details, or provider diagnostics.
/// </summary>
public sealed class DurableBrowserVerificationReceiptStore
{
    private const string ProtectionPurpose = "browser-verification-receipt/v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _path;
    private readonly ILocalStateProtector _protector;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public DurableBrowserVerificationReceiptStore(string path, ILocalStateProtector protector)
    {
        _path = AtomicTextArtifactWriter.ValidateDestination(path, "Browser verification receipt");
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
    }

    public async Task WriteAsync(
        DurableBrowserVerificationReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        if (!receipt.HasValidIntegrity())
            throw new InvalidDataException("Browser verification receipt failed integrity validation before persistence.");

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var json = JsonSerializer.SerializeToUtf8Bytes(receipt, JsonOptions);
            try
            {
                var envelope = LocalStateEnvelope.Encode(json, _protector, ProtectionPurpose);
                cancellationToken.ThrowIfCancellationRequested();
                AtomicTextArtifactWriter.Write(_path, Encoding.UTF8.GetString(envelope), "Browser verification receipt");
            }
            finally
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(json);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DurableBrowserVerificationReceipt?> ReadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_path))
                return null;

            var persisted = await File.ReadAllBytesAsync(_path, cancellationToken).ConfigureAwait(false);
            try
            {
                if (!LocalStateEnvelope.HasProtectedHeader(persisted))
                    throw new InvalidDataException("Browser verification receipt is not protected local state.");

                var payload = LocalStateEnvelope.Decode(persisted, _protector, ProtectionPurpose);
                if (!payload.WasProtected)
                    throw new InvalidDataException("Browser verification receipt protection is required.");

                try
                {
                    var receipt = JsonSerializer.Deserialize<DurableBrowserVerificationReceipt>(payload.Plaintext, JsonOptions)
                        ?? throw new InvalidDataException("Browser verification receipt is empty or malformed.");
                    if (!receipt.HasValidIntegrity())
                        throw new InvalidDataException("Browser verification receipt failed integrity validation.");
                    return receipt;
                }
                catch (JsonException ex)
                {
                    throw new InvalidDataException("Browser verification receipt contains invalid JSON.", ex);
                }
                finally
                {
                    System.Security.Cryptography.CryptographicOperations.ZeroMemory(payload.Plaintext);
                }
            }
            finally
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(persisted);
            }
        }
        finally
        {
            _gate.Release();
        }
    }
}
