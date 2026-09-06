namespace Nvidea.Core.Browser;

public sealed class BrowserSafetyPolicy
{
    private static readonly string[] ConsequentialTerms =
    {
        "submit", "send", "publish", "post", "delete", "remove", "purchase", "buy", "checkout",
        "pay", "confirm order", "place order", "transfer", "withdraw", "close account", "change password",
        "reset password", "authorize", "approve", "sign", "unsubscribe"
    };

    private static readonly string[] SensitiveFieldTerms =
    {
        "password", "passcode", "otp", "one-time", "2fa", "mfa", "cvv", "cvc", "card number",
        "credit card", "debit card", "bank account", "routing number", "private key", "secret", "api key"
    };

    public BrowserActionDecision Evaluate(BrowserAction action, BrowserObservation observation)
    {
        if (action.Kind == BrowserActionKind.Navigate && action.Destination is not null)
        {
            if (!IsSafeWebUri(action.Destination))
                return Block("Navigation is limited to HTTPS/HTTP web URLs; script/data/file schemes are blocked.");
        }

        if (action.Kind == BrowserActionKind.Upload)
            return High("Uploading local data crosses a trust boundary and always requires explicit approval.");

        var target = BuildTargetText(action);
        if (ContainsAny(target, SensitiveFieldTerms))
        {
            if (action.Kind == BrowserActionKind.Type)
                return Block("Typing secrets, credentials, OTPs, payment credentials, or private keys through the autonomous browser agent is blocked.");

            return High("The target appears security- or credential-sensitive and requires explicit approval.");
        }

        if (ContainsAny(target, ConsequentialTerms))
            return High("The action appears consequential (send/submit/publish/delete/purchase/account change) and requires explicit approval.");

        if (observation.ContainsUntrustedInstructions && action.Kind is BrowserActionKind.Type or BrowserActionKind.Click)
            return Medium("The page contains untrusted-instruction indicators; interaction is allowed only under the agent's trusted plan and verification loop.");

        return action.Kind switch
        {
            BrowserActionKind.Read => Low("Read-only observation."),
            BrowserActionKind.Back or BrowserActionKind.Refresh => Low("Reversible navigation action."),
            BrowserActionKind.Navigate => Medium("External navigation changes browsing context."),
            BrowserActionKind.Click => Medium("Click changes page state and must be verified."),
            BrowserActionKind.Type or BrowserActionKind.Select => Medium("Form interaction changes page state and must be verified."),
            BrowserActionKind.Download => Medium("Download creates a local artifact and must be constrained by the driver."),
            _ => Block("Unsupported or unclassified browser action.")
        };
    }

    private static BrowserActionDecision Low(string reason) =>
        new(BrowserRiskLevel.Low, RequiresApproval: false, Allowed: true, reason);

    private static BrowserActionDecision Medium(string reason) =>
        new(BrowserRiskLevel.Medium, RequiresApproval: false, Allowed: true, reason);

    private static BrowserActionDecision High(string reason) =>
        new(BrowserRiskLevel.High, RequiresApproval: true, Allowed: true, reason);

    private static BrowserActionDecision Block(string reason) =>
        new(BrowserRiskLevel.Blocked, RequiresApproval: false, Allowed: false, reason);

    private static bool IsSafeWebUri(Uri uri) =>
        uri.IsAbsoluteUri && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    private static string BuildTargetText(BrowserAction action)
    {
        return string.Join(' ', new[]
        {
            action.Locator?.Value,
            action.Locator?.Name,
            action.Locator?.Role,
            action.ExpectedState,
            action.Rationale
        }.Where(static value => !string.IsNullOrWhiteSpace(value)));
    }

    private static bool ContainsAny(string haystack, IEnumerable<string> needles) =>
        needles.Any(needle => haystack.Contains(needle, StringComparison.OrdinalIgnoreCase));
}
