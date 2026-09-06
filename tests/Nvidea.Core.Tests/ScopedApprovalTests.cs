using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Tests;

public sealed class ScopedApprovalTests
{
    [Fact]
    public void ApprovalGrant_AuthorizesExactScopeOnce()
    {
        var authorizer = new ScopedApprovalAuthorizer();
        var decision = Decision("browser.form|action-a|BrowserWrite");
        var grant = authorizer.Grant(decision, TimeSpan.FromMinutes(2));

        Assert.True(authorizer.TryAuthorize(grant, decision, grant.GrantedAt.AddSeconds(1)));
        Assert.False(authorizer.TryAuthorize(grant, decision, grant.GrantedAt.AddSeconds(2)));
    }

    [Fact]
    public void ApprovalGrant_CannotAuthorizeDifferentActionScope()
    {
        var authorizer = new ScopedApprovalAuthorizer();
        var original = Decision("browser.form|action-a|BrowserWrite");
        var different = Decision("browser.form|action-b|BrowserWrite");
        var grant = authorizer.Grant(original, TimeSpan.FromMinutes(2));

        Assert.False(authorizer.TryAuthorize(grant, different, grant.GrantedAt.AddSeconds(1)));
    }

    [Fact]
    public void ApprovalGrant_RejectsExpiredGrant()
    {
        var authorizer = new ScopedApprovalAuthorizer();
        var decision = Decision("email.send|send-1|EmailSend");
        var grant = authorizer.Grant(decision, TimeSpan.FromSeconds(30));

        Assert.False(authorizer.TryAuthorize(grant, decision, grant.ExpiresAt.AddSeconds(1)));
    }

    [Fact]
    public void ApprovalGrant_LifetimeIsBounded()
    {
        var authorizer = new ScopedApprovalAuthorizer();
        var decision = Decision("email.send|send-1|EmailSend");

        Assert.Throws<ArgumentOutOfRangeException>(() => authorizer.Grant(decision, TimeSpan.FromHours(1)));
    }

    private static PermissionDecision Decision(string scope) => new(
        Allowed: true,
        RequiresApproval: true,
        EffectiveRisk: CapabilityRiskLevel.High,
        EffectivePermissions: new HashSet<DataPermission> { DataPermission.BrowserWrite },
        Reason: "test",
        ApprovalScope: scope);
}
