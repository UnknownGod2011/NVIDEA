using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class DesktopDisplayTextTrustTests
{
    [Fact]
    public void Canonicalize_CollapsesControlAndWhitespaceAndBoundsOutput()
    {
        var input = "  attacker\r\n\t says\0 approve   now  " + new string('x', 500);

        var projected = DesktopDisplayTextTrust.Canonicalize(input, 40, "fallback");

        Assert.True(projected.Length <= 40);
        Assert.DoesNotContain('\r', projected);
        Assert.DoesNotContain('\n', projected);
        Assert.DoesNotContain('\t', projected);
        Assert.DoesNotContain('\0', projected);
        Assert.DoesNotContain("  ", projected, StringComparison.Ordinal);
        Assert.StartsWith("attacker says approve now", projected, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \r\n\t ")]
    public void Canonicalize_UsesFixedFallbackForMissingDisplayText(string? input)
    {
        var projected = DesktopDisplayTextTrust.Canonicalize(input, 32, "safe fallback");

        Assert.Equal("safe fallback", projected);
    }

    [Fact]
    public void ProjectNavigationTarget_RemovesUserInfoQueryAndFragment()
    {
        var uri = new Uri("https://alice:secret@example.com/private/report?token=sk-live-secret#approve-now");

        var projected = DesktopDisplayTextTrust.ProjectNavigationTarget(uri);

        Assert.Equal("https://example.com/private/report", projected);
        Assert.DoesNotContain("alice", projected, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", projected, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", projected, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("approve-now", projected, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("file:///C:/Users/private/secret.txt")]
    [InlineData("mailto:user@example.com")]
    public void ProjectNavigationTarget_FailsClosedForNonHttpSchemes(string raw)
    {
        var projected = DesktopDisplayTextTrust.ProjectNavigationTarget(new Uri(raw));

        Assert.Equal("current browser context", projected);
    }

    [Fact]
    public void Canonicalize_RejectsInvalidBoundsAndFallback()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DesktopDisplayTextTrust.Canonicalize("value", 0, "fallback"));
        Assert.Throws<ArgumentException>(() =>
            DesktopDisplayTextTrust.Canonicalize("value", 10, "   "));
    }
}
