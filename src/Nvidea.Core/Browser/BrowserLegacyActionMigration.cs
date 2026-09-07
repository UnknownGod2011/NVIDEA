namespace Nvidea.Core.Browser;

public enum BrowserLegacyMigrationStatus
{
    NotNeeded,
    Migrated,
    RequiresHumanReview
}

public sealed record BrowserLegacyMigrationResult(
    BrowserLegacyMigrationStatus Status,
    BrowserAction? Action,
    string Reason);

/// <summary>
/// Conservative migration helper for browser actions persisted before typed postconditions
/// became the autonomous verification contract. It never guesses the meaning of arbitrary
/// free-text ExpectedState values. Only cases with an independently deterministic typed
/// equivalent are upgraded automatically.
/// </summary>
public static class BrowserLegacyActionMigration
{
    public static BrowserLegacyMigrationResult Migrate(BrowserAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var hasLegacyExpectedState = !string.IsNullOrWhiteSpace(action.ExpectedState);
        if (!hasLegacyExpectedState)
        {
            return new BrowserLegacyMigrationResult(
                BrowserLegacyMigrationStatus.NotNeeded,
                action,
                "Action does not contain legacy free-text verification.");
        }

        if (action.Postconditions is { Count: > 0 })
        {
            return new BrowserLegacyMigrationResult(
                BrowserLegacyMigrationStatus.Migrated,
                action with { ExpectedState = null },
                "Removed redundant legacy ExpectedState because typed postconditions are already present.");
        }

        if (action.Kind == BrowserActionKind.Navigate
            && action.Destination is { IsAbsoluteUri: true } destination
            && IsHttpOrHttps(destination))
        {
            return new BrowserLegacyMigrationResult(
                BrowserLegacyMigrationStatus.Migrated,
                action with
                {
                    ExpectedState = null,
                    Postconditions = new[]
                    {
                        new BrowserPostcondition(
                            BrowserPostconditionKind.UrlEquals,
                            ExpectedText: destination.AbsoluteUri)
                    }
                },
                "Replaced legacy navigation verification with an exact typed destination-URL postcondition.");
        }

        return new BrowserLegacyMigrationResult(
            BrowserLegacyMigrationStatus.RequiresHumanReview,
            null,
            "Legacy free-text verification cannot be converted deterministically. The action must not be replayed or autonomously resumed until a typed postcondition is supplied by trusted code or a human reviews the interrupted work.");
    }

    public static void EnsureAutonomousActionUsesTypedVerification(BrowserAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!string.IsNullOrWhiteSpace(action.ExpectedState))
        {
            throw new InvalidOperationException(
                "Autonomous browser actions may not use legacy ExpectedState verification. Use typed Postconditions instead.");
        }

        if (RequiresPostcondition(action.Kind) && action.Postconditions is not { Count: > 0 })
        {
            throw new InvalidOperationException(
                $"Autonomous browser action '{action.Kind}' requires at least one typed postcondition.");
        }
    }

    private static bool RequiresPostcondition(BrowserActionKind kind) => kind is
        BrowserActionKind.Navigate
        or BrowserActionKind.Click
        or BrowserActionKind.Type
        or BrowserActionKind.Select
        or BrowserActionKind.Upload
        or BrowserActionKind.Download
        or BrowserActionKind.Back
        or BrowserActionKind.Refresh;

    private static bool IsHttpOrHttps(Uri uri) =>
        string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
