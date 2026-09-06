namespace Nvidea.Core.Capabilities;

public sealed record ApprovalGrant(
    Guid GrantId,
    string ApprovalScope,
    DateTimeOffset GrantedAt,
    DateTimeOffset ExpiresAt,
    bool SingleUse = true);

public sealed class ScopedApprovalAuthorizer
{
    private readonly Dictionary<Guid, ApprovalGrant> _grants = new();
    private readonly HashSet<Guid> _consumed = new();
    private readonly object _sync = new();

    public ApprovalGrant Grant(PermissionDecision decision, TimeSpan lifetime)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (!decision.Allowed || !decision.RequiresApproval || string.IsNullOrWhiteSpace(decision.ApprovalScope))
            throw new InvalidOperationException("Only allowed decisions requiring approval can receive a grant.");

        return GrantExactScope(decision.ApprovalScope, lifetime);
    }

    /// <summary>
    /// Creates a short-lived single-use grant after an explicit user approval path has
    /// already verified the exact paused action scope. Internal-only so arbitrary
    /// capability consumers cannot mint their own approvals.
    /// </summary>
    internal ApprovalGrant GrantExactScope(string approvalScope, TimeSpan lifetime)
    {
        if (string.IsNullOrWhiteSpace(approvalScope))
            throw new ArgumentException("Approval scope is required.", nameof(approvalScope));
        if (lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromMinutes(15))
            throw new ArgumentOutOfRangeException(nameof(lifetime), "Approval lifetime must be greater than zero and no more than 15 minutes.");

        var now = DateTimeOffset.UtcNow;
        var grant = new ApprovalGrant(Guid.NewGuid(), approvalScope.Trim(), now, now.Add(lifetime));
        lock (_sync)
            _grants[grant.GrantId] = grant;
        return grant;
    }

    public bool TryAuthorize(ApprovalGrant grant, PermissionDecision decision, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(grant);
        ArgumentNullException.ThrowIfNull(decision);
        var timestamp = now ?? DateTimeOffset.UtcNow;

        lock (_sync)
        {
            if (!_grants.TryGetValue(grant.GrantId, out var stored))
                return false;
            if (_consumed.Contains(grant.GrantId))
                return false;
            if (!string.Equals(stored.ApprovalScope, decision.ApprovalScope, StringComparison.Ordinal))
                return false;
            if (!decision.Allowed || !decision.RequiresApproval)
                return false;
            if (timestamp < stored.GrantedAt || timestamp > stored.ExpiresAt)
                return false;

            if (stored.SingleUse)
                _consumed.Add(stored.GrantId);
            return true;
        }
    }
}
