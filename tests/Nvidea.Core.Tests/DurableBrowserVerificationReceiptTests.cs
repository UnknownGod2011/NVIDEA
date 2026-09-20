using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class DurableBrowserVerificationReceiptTests
{
    [Fact]
    public void Create_ProducesIntegrityBoundPayloadFreeVerifiedPresentation()
    {
        const string privateMarker = "PRIVATE_TYPED_VALUE_SHOULD_NOT_PERSIST";
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");
        var receipt = new BrowserActionReceipt(
            Guid.NewGuid(),
            new BrowserAction(BrowserActionKind.Click, BrowserLocator.ByRole("button", privateMarker), Value: privateMarker),
            new BrowserActionDecision(BrowserRiskLevel.High, true, true, "private policy reason"),
            now,
            now.AddSeconds(1),
            true,
            true,
            "private verification detail",
            new Uri("https://private.example/before"),
            new Uri("https://private.example/after"),
            ApprovalGranted: true);

        var durable = DurableBrowserVerificationReceipt.Create(new[] { receipt });
        var presentation = durable.ToPresentation();
        var serialized = System.Text.Json.JsonSerializer.Serialize(durable);

        Assert.True(durable.HasValidIntegrity());
        Assert.True(presentation.Verified);
        Assert.Equal(1, presentation.ActionCount);
        Assert.Equal(1, presentation.ApprovalCount);
        Assert.DoesNotContain(privateMarker, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("private.example", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("verification detail", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("policy reason", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TamperedStructuralEvidence_FailsClosed()
    {
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");
        var source = new BrowserActionReceipt(
            Guid.NewGuid(),
            new BrowserAction(BrowserActionKind.Navigate),
            new BrowserActionDecision(BrowserRiskLevel.Low, false, true, "allowed"),
            now,
            now.AddSeconds(1),
            true,
            true,
            "verified",
            new Uri("https://example.com/a"),
            new Uri("https://example.com/b"));
        var durable = DurableBrowserVerificationReceipt.Create(new[] { source });
        var tampered = durable with
        {
            Actions = durable.Actions.Select(static x => x with { PostStateVerified = false }).ToArray()
        };

        Assert.False(tampered.HasValidIntegrity());
        Assert.False(tampered.ToPresentation().Verified);
    }

    [Fact]
    public void ApprovalObservedWithoutRequirement_IsRejected()
    {
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");
        var source = new BrowserActionReceipt(
            Guid.NewGuid(),
            new BrowserAction(BrowserActionKind.Read),
            new BrowserActionDecision(BrowserRiskLevel.Low, false, true, "allowed"),
            now,
            now.AddSeconds(1),
            true,
            true,
            "verified",
            new Uri("https://example.com"),
            new Uri("https://example.com"),
            ApprovalGranted: true);

        Assert.Throws<InvalidDataException>(() => DurableBrowserVerificationReceipt.Create(new[] { source }));
    }
}
