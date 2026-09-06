using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Jobs;

/// <summary>
/// In-memory approval handoff for resumed jobs. Grants are deliberately absent from
/// AgentJobRecord and checkpoint contracts so a process restart always loses them.
/// </summary>
public sealed class EphemeralJobApprovalStore
{
    private readonly Dictionary<Guid, ApprovalGrant> _grants = new();
    private readonly object _sync = new();

    public void Put(Guid jobId, ApprovalGrant grant)
    {
        ArgumentNullException.ThrowIfNull(grant);
        lock (_sync)
            _grants[jobId] = grant;
    }

    public ApprovalGrant? Take(Guid jobId)
    {
        lock (_sync)
        {
            if (!_grants.Remove(jobId, out var grant))
                return null;
            return grant;
        }
    }

    public void Revoke(Guid jobId)
    {
        lock (_sync)
            _grants.Remove(jobId);
    }
}

/// <summary>
/// Per-step execution context. The approval can be taken at most once and only for
/// the exact scope that was approved. Taking it removes it from this context even if
/// the downstream action later fails, forcing fresh consent for ambiguous retries.
/// </summary>
public sealed class JobExecutionContext
{
    private ApprovalGrant? _approval;

    internal JobExecutionContext(Guid jobId, ApprovalGrant? approval)
    {
        JobId = jobId;
        _approval = approval;
    }

    public Guid JobId { get; }

    public ApprovalGrant? TakeApproval(string exactApprovalScope)
    {
        if (string.IsNullOrWhiteSpace(exactApprovalScope))
            return null;

        var current = _approval;
        if (current is null)
            return null;
        if (!string.Equals(current.ApprovalScope, exactApprovalScope, StringComparison.Ordinal))
            return null;

        _approval = null;
        return current;
    }
}
