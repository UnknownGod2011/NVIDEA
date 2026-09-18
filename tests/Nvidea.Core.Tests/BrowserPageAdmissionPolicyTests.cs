using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class BrowserPageAdmissionPolicyTests
{
    [Theory]
    [InlineData("https://example.com/path")]
    [InlineData("http://localhost:8123/path")]
    [InlineData("http://127.0.0.1:8123/path")]
    [InlineData("http://[::1]:8123/path")]
    public void IsAllowed_AcceptsSecureOrLoopbackWebDestinations(string raw)
    {
        var policy = new BrowserPageAdmissionPolicy();
        Assert.True(policy.IsAllowed(raw));
    }

    [Theory]
    [InlineData("http://example.com/path")]
    [InlineData("https://user:password@example.com/path")]
    [InlineData("http://token@localhost:8123/path")]
    [InlineData("file:///C:/secret.txt")]
    [InlineData("about:blank")]
    [InlineData("javascript:alert(1)")]
    [InlineData("not-a-url")]
    public void IsAllowed_RejectsTransportUnsafeOrCredentialBearingDestinations(string raw)
    {
        var policy = new BrowserPageAdmissionPolicy();
        Assert.False(policy.IsAllowed(raw));
    }

    [Fact]
    public void IsAllowed_ComposesTransportPolicyWithHostAllowlist()
    {
        var policy = new BrowserPageAdmissionPolicy(new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "allowed.example"
        });

        Assert.True(policy.IsAllowed("https://allowed.example/task"));
        Assert.True(policy.IsAllowed("https://ALLOWED.EXAMPLE/task"));
        Assert.False(policy.IsAllowed("https://other.example/task"));
        Assert.False(policy.IsAllowed("http://allowed.example/task"));
        Assert.False(policy.IsAllowed("https://token@allowed.example/task"));
    }
}
