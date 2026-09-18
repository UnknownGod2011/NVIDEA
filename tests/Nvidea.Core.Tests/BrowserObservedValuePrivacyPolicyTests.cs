using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class BrowserObservedValuePrivacyPolicyTests
{
    [Theory]
    [InlineData("password", null)]
    [InlineData("PASSWORD", null)]
    [InlineData("text", "current-password")]
    [InlineData("text", "new-password")]
    [InlineData("text", "one-time-code")]
    [InlineData("text", "cc-number")]
    [InlineData("text", "cc-csc")]
    [InlineData("text", "cc-exp")]
    [InlineData("text", "cc-exp-month")]
    [InlineData("text", "cc-exp-year")]
    [InlineData("text", "section-checkout billing cc-number")]
    public void SensitiveFormSemantics_SuppressObservedValue(string? inputType, string? autoComplete)
    {
        Assert.True(BrowserObservedValuePrivacyPolicy.ShouldSuppressValue(inputType, autoComplete));
    }

    [Theory]
    [InlineData("text", null)]
    [InlineData("text", "off")]
    [InlineData("email", "email")]
    [InlineData("text", "username")]
    [InlineData("text", "organization")]
    [InlineData("text", "street-address")]
    [InlineData("text", "section-checkout shipping street-address")]
    [InlineData("text", "cc-name")]
    [InlineData("text", "cc-type")]
    [InlineData("text", "one-time-coder")]
    public void BenignFormSemantics_DoNotSuppressObservedValue(string? inputType, string? autoComplete)
    {
        Assert.False(BrowserObservedValuePrivacyPolicy.ShouldSuppressValue(inputType, autoComplete));
    }
}
