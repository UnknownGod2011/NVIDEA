using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class BrowserGoalSessionStoreTests
{
    [Fact]
    public async Task SaveAsync_StripsPendingActionValueAndPersistsOnlyDescriptiveApprovalState()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-goal-store-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "goal-sessions.json");
        try
        {
            var store = new JsonBrowserGoalSessionStore(path);
            var session = BrowserGoalSession.Create("Submit the draft") with
            {
                Status = BrowserGoalStatus.WaitingForApproval,
                PendingJobId = Guid.NewGuid(),
                PendingExactScope = "capability:browser.agent:submit:exact",
                PendingAction = new BrowserAction(
                    BrowserActionKind.Type,
                    BrowserLocator.Accessibility("e-7"),
                    Value: "private-user-value")
            };

            await store.SaveAsync(session);

            var json = await File.ReadAllTextAsync(path);
            Assert.Contains("capability:browser.agent:submit:exact", json, StringComparison.Ordinal);
            Assert.DoesNotContain("private-user-value", json, StringComparison.Ordinal);
            Assert.DoesNotContain("approvalGrant", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("grantId", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("bearer", json, StringComparison.OrdinalIgnoreCase);

            var restored = await store.GetAsync(session.SessionId);
            Assert.NotNull(restored);
            Assert.Equal(BrowserGoalStatus.WaitingForApproval, restored!.Status);
            Assert.Equal(session.PendingJobId, restored.PendingJobId);
            Assert.Equal(session.PendingExactScope, restored.PendingExactScope);
            Assert.Null(restored.PendingAction);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_RoundTripsBudgetsAndPrivacyMinimizedVerifiedHistory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-goal-store-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "goal-sessions.json");
        try
        {
            var store = new JsonBrowserGoalSessionStore(path);
            var verified = new BrowserGoalVerifiedStep(
                Guid.NewGuid(),
                BrowserActionKind.Click,
                new Uri("https://example.com/before"),
                new Uri("https://example.com/after"),
                "Panel opened and verified.",
                DateTimeOffset.UtcNow);
            var session = BrowserGoalSession.Create(
                "Open details",
                maxActions: 7,
                maxPlannerTurns: 9,
                maxPlannerContextCharacters: 50_000,
                maxWallClockSeconds: 300) with
            {
                ActionCount = 1,
                PlannerTurnCount = 2,
                PlannerContextCharacters = 4_321,
                VerifiedSteps = new[] { verified }
            };

            await store.SaveAsync(session);
            var restored = await store.GetAsync(session.SessionId);

            Assert.NotNull(restored);
            Assert.Equal(7, restored!.MaxActions);
            Assert.Equal(9, restored.MaxPlannerTurns);
            Assert.Equal(4_321, restored.PlannerContextCharacters);
            Assert.Equal(50_000, restored.MaxPlannerContextCharacters);
            Assert.Equal(300, restored.MaxWallClockSeconds);
            var step = Assert.Single(restored.VerifiedSteps!);
            Assert.Equal(verified, step);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
