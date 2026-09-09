using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Explicit composition boundary for a live Nebius research runtime. Unlike the lower-level
/// NebiusResearchClientRuntime.Create method used by unit/contract fixtures, this entry point
/// refuses to construct a production remote runtime until the worker transport and secret-reference
/// topology has passed NebiusResearchDeploymentPreflight.
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
        IAuditTrail auditTrail)
    {
        ArgumentNullException.ThrowIfNull(options);
        NebiusResearchDeploymentPreflight.Validate(options);

        return NebiusResearchClientRuntime.Create(
            store,
            serverless,
            workItems,
            results,
            bindings,
            options,
            clientPrivateKeyPem,
            auditTrail);
    }
}
