namespace Nvidea.Core.Jobs;

/// <summary>
/// Client-side transport boundary that authenticates worker origin while the remote research
/// result is still encrypted. The pinned verification key comes from trusted client composition,
/// never from the result envelope. A result with no worker signature fails closed.
///
/// This decorator intentionally performs no decryption and therefore can sit immediately around
/// Object Storage/directory transports. RemoteResearchResultIngestor can consume it without giving
/// unauthenticated remote bytes access to the client private key or JSON result parser.
/// </summary>
public sealed class AuthenticatedResearchResultTransport : IProtectedResearchResultTransport
{
    private readonly IProtectedResearchResultTransport _inner;
    private readonly string _pinnedWorkerPublicKeyPem;

    public AuthenticatedResearchResultTransport(
        IProtectedResearchResultTransport inner,
        string pinnedWorkerPublicKeyPem)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _pinnedWorkerPublicKeyPem = WorkerResultVerificationPublicKeyTrust.ValidateAndCanonicalize(
            pinnedWorkerPublicKeyPem);
    }

    public Task PutAsync(
        ProtectedResearchResultEnvelope envelope,
        CancellationToken cancellationToken = default) =>
        _inner.PutAsync(envelope, cancellationToken);

    public async Task<ProtectedResearchResultEnvelope?> GetAsync(
        string opaqueWorkItemId,
        CancellationToken cancellationToken = default)
    {
        var envelope = await _inner.GetAsync(opaqueWorkItemId, cancellationToken).ConfigureAwait(false);
        if (envelope is null)
            return null;

        if (!string.Equals(envelope.OpaqueWorkItemId, opaqueWorkItemId, StringComparison.Ordinal))
            throw new InvalidOperationException("Remote result transport returned substituted work-item provenance.");

        RemoteResearchWorkerSignature.Verify(
            envelope,
            envelope.WorkerSignature ?? string.Empty,
            _pinnedWorkerPublicKeyPem);
        return envelope;
    }

    public Task DeleteAsync(
        string opaqueWorkItemId,
        CancellationToken cancellationToken = default) =>
        _inner.DeleteAsync(opaqueWorkItemId, cancellationToken);
}
