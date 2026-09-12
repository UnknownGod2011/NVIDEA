using Nvidea.Core.Capabilities;
using System.Security.Cryptography;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Explicit composition boundary for a live Nebius research runtime. Unlike the lower-level
/// NebiusResearchClientRuntime.Create method used by unit/contract fixtures, this entry point
/// refuses to construct a production remote runtime until the worker transport and secret-reference
/// topology has passed NebiusResearchDeploymentPreflight and the client signing/result identities
/// have been proven distinct.
/// </summary>
public static class NebiusResearchLiveRuntimeFactory
{
    public static NebiusResearchClientRuntime Create(
        JsonAgentJobStore store,
        INebiusServerlessJobClient serverless,
        IProtectedResearchWorkItemTransport workItems,
        IProtectedResearchResultTransport results,
        IProtectedResearchDispatchBindingTransport bindings,
        NebiusResearchDispatchOptions options,
        string clientPrivateKeyPem,
        string clientResultPrivateKeyPem,
        IAuditTrail auditTrail)
    {
        ArgumentNullException.ThrowIfNull(options);
        NebiusResearchDeploymentPreflight.Validate(options);

        var signingPublicKey = NebiusResearchLiveDryRunPreflight
            .ValidateAndDeriveClientPublicKey(clientPrivateKeyPem);
        var canonicalResultPrivateKey = ClientResultEnvelopePrivateKeyTrust
            .ValidateAndCanonicalize(clientResultPrivateKeyPem);
        using var resultRsa = ClientResultEnvelopePrivateKeyTrust.CreateValidatedRsa(canonicalResultPrivateKey);
        var resultPublicKey = resultRsa.ExportSubjectPublicKeyInfoPem();

        if (string.Equals(signingPublicKey, resultPublicKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Live Nebius research requires distinct RSA identities for dispatch signing and result-envelope decryption.");
        }

        return NebiusResearchClientRuntime.Create(
            store,
            serverless,
            workItems,
            results,
            bindings,
            options,
            clientPrivateKeyPem,
            auditTrail,
            canonicalResultPrivateKey);
    }
}
