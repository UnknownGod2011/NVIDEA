using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class BrowserLegacyActionMigrationTests
{
    [Fact]
    public void LegacyNavigation_WithTrustedDestination_MigratesToExactUrlPostcondition()
    {
        var destination = new Uri("https://example.test/account/complete");
        var legacy = new BrowserAction(
            BrowserActionKind.Navigate,
            Destination: destination,
            ExpectedState: "Account complete");

        var result = BrowserLegacyActionMigration.Migrate(legacy);

        Assert.Equal(BrowserLegacyMigrationStatus.Migrated, result.Status);
        Assert.NotNull(result.Action);
        Assert.Null(result.Action!.ExpectedState);
        var postcondition = Assert.Single(result.Action.Postconditions!);
        Assert.Equal(BrowserPostconditionKind.UrlEquals, postcondition.Kind);
        Assert.Equal(destination.AbsoluteUri, postcondition.Expected);
    }

    [Fact]
    public void LegacyMutation_WithoutDeterministicTypedEquivalent_RequiresHumanReview()
    {
        var legacy = new BrowserAction(
            BrowserActionKind.Click,
            BrowserLocator.ByRole("button", "Submit"),
            ExpectedState: "Submission succeeded");

        var result = BrowserLegacyActionMigration.Migrate(legacy);

        Assert.Equal(BrowserLegacyMigrationStatus.RequiresHumanReview, result.Status);
        Assert.Null(result.Action);
        Assert.Contains("must not be replayed", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LegacyExpectedState_WithExistingTypedPostconditions_IsRemovedWithoutChangingTypedContract()
    {
        var typed = new BrowserPostcondition(
            BrowserPostconditionKind.VisibleTextContains,
            Expected: "saved");
        var legacy = new BrowserAction(
            BrowserActionKind.Click,
            BrowserLocator.ByRole("button", "Save"),
            ExpectedState: "Saved",
            Postconditions: new[] { typed });

        var result = BrowserLegacyActionMigration.Migrate(legacy);

        Assert.Equal(BrowserLegacyMigrationStatus.Migrated, result.Status);
        Assert.NotNull(result.Action);
        Assert.Null(result.Action!.ExpectedState);
        Assert.Equal(new[] { typed }, result.Action.Postconditions);
    }

    [Fact]
    public void AutonomousGuard_RejectsLegacyExpectedStateEvenWhenTypedPredicatesAlsoExist()
    {
        var action = new BrowserAction(
            BrowserActionKind.Click,
            BrowserLocator.ByRole("button", "Save"),
            ExpectedState: "Saved",
            Postconditions: new[]
            {
                new BrowserPostcondition(BrowserPostconditionKind.VisibleTextContains, Expected: "saved")
            });

        var error = Assert.Throws<InvalidOperationException>(() =>
            BrowserLegacyActionMigration.EnsureAutonomousActionUsesTypedVerification(action));

        Assert.Contains("ExpectedState", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AutonomousGuard_RejectsWriteWithoutTypedPostcondition()
    {
        var action = new BrowserAction(
            BrowserActionKind.Click,
            BrowserLocator.ByRole("button", "Continue"));

        var error = Assert.Throws<InvalidOperationException>(() =>
            BrowserLegacyActionMigration.EnsureAutonomousActionUsesTypedVerification(action));

        Assert.Contains("typed postcondition", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AutonomousGuard_AllowsReadWithoutPostcondition()
    {
        var action = new BrowserAction(BrowserActionKind.Read);

        BrowserLegacyActionMigration.EnsureAutonomousActionUsesTypedVerification(action);
    }
}
