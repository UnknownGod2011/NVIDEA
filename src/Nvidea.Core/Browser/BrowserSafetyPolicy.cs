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
        "password", "passcode", "pin", "otp", "one-time", "one time password", "verification code", "security code",
        "2fa", "mfa", "cvv", "cvc", "card number", "credit card", "debit card", "bank account", "routing number",
        "private key", "seed phrase", "recovery phrase", "recovery code", "backup code", "secret", "api key",
        "access token", "refresh token", "bearer token", "social security number", "ssn",
        // Standard HTML autocomplete tokens are browser-authored form semantics rather than model rationale.
        // Including them here protects CSS/attribute locators even when a site deliberately gives the field
        // an innocuous or empty visible label. Keep explicit variants rather than broad substring matching.
        "current-password", "new-password", "one-time-code", "cc-number", "cc-csc", "cc-exp", "cc-exp-month",
        "cc-exp-year", "cc-name", "transaction-amount", "transaction-currency"
    };

    public BrowserActionDecision Evaluate(BrowserAction action, BrowserObservation observation)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(observation);

        if (action.Kind == BrowserActionKind.Navigate && action.Destination is not null)
        {
            if (!IsSafeWebUri(action.Destination))
                return Block("Navigation requires HTTPS without embedded URL credentials. Plain HTTP is allowed only for loopback development endpoints; script/data/file, credential-bearing, and remote plaintext URLs are blocked.");
        }

        if (action.Kind == BrowserActionKind.Upload)
            return High("Uploading local data crosses a trust boundary and always requires explicit approval.");

        var target = BuildTargetText(action, observation);
        if (ContainsAny(target, SensitiveFieldTerms))
        {
            if (action.Kind == BrowserActionKind.Type)
                return Block("Typing secrets, credentials, OTPs, payment credentials, identity numbers, recovery material, or private keys through the autonomous browser agent is blocked.");

            return High("The target appears security- or credential-sensitive and requires explicit approval.");
        }

        if (ContainsAny(target, ConsequentialTerms))
            return High("The action appears consequential (send/submit/publish/delete/purchase/account change) and requires explicit approval.");

        if (observation.ContainsUntrustedInstructions
            && action.Kind is BrowserActionKind.Navigate
                or BrowserActionKind.Click
                or BrowserActionKind.Type
                or BrowserActionKind.Select
                or BrowserActionKind.Download)
        {
            return High("The page contains prompt-injection-like instructions; state-changing interaction requires explicit user approval even when the target is otherwise non-consequential.");
        }

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

    public BrowserActionDecision EvaluateObservedLocation(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return IsSafeWebUri(uri)
            ? Low("Observed browser location uses an allowed transport.")
            : Block("Browser reached an unsafe location after an action. Further autonomous interaction is blocked.");
    }

    public BrowserActionDecision EvaluateWebSocketTransport(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return IsSafeWebSocketUri(uri)
            ? Low("WebSocket uses an allowed transport.")
            : Block("Credential-bearing, remote plaintext, or otherwise unsafe WebSocket transport is blocked.");
    }

    private static BrowserActionDecision Low(string reason) => new(BrowserRiskLevel.Low, false, true, reason);
    private static BrowserActionDecision Medium(string reason) => new(BrowserRiskLevel.Medium, false, true, reason);
    private static BrowserActionDecision High(string reason) => new(BrowserRiskLevel.High, true, true, reason);
    private static BrowserActionDecision Block(string reason) => new(BrowserRiskLevel.Blocked, false, false, reason);

    private static bool IsSafeWebUri(Uri uri)
    {
        if (!uri.IsAbsoluteUri || HasEmbeddedCredentials(uri)) return false;
        if (uri.Scheme == Uri.UriSchemeHttps) return true;
        return uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback;
    }

    private static bool IsSafeWebSocketUri(Uri uri)
    {
        if (!uri.IsAbsoluteUri || HasEmbeddedCredentials(uri)) return false;
        if (string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase)) return true;
        return string.Equals(uri.Scheme, "ws", StringComparison.OrdinalIgnoreCase) && uri.IsLoopback;
    }

    private static bool HasEmbeddedCredentials(Uri uri) => !string.IsNullOrEmpty(uri.UserInfo);

    private static string BuildTargetText(BrowserAction action, BrowserObservation observation)
    {
        var parts = new List<string?>
        {
            action.Locator?.Value, action.Locator?.Name, action.Locator?.Role, action.ExpectedState, action.Rationale
        };

        if (action.Locator?.Kind == BrowserLocatorKind.AccessibilityRef)
        {
            var observed = observation.Elements.FirstOrDefault(element =>
                string.Equals(element.Reference, action.Locator.Value, StringComparison.Ordinal));
            if (observed is not null)
            {
                parts.Add(observed.Name);
                parts.Add(observed.Role);
                parts.Add(observed.Value);
            }
        }

        return string.Join(' ', parts.Where(static value => !string.IsNullOrWhiteSpace(value)));
    }

    private static bool ContainsAny(string haystack, IEnumerable<string> needles) =>
        needles.Any(needle => ContainsBoundedTerm(haystack, needle));

    private static bool ContainsBoundedTerm(string haystack, string term)
    {
        if (string.IsNullOrWhiteSpace(haystack) || string.IsNullOrWhiteSpace(term)) return false;
        var searchFrom = 0;
        while (searchFrom <= haystack.Length - term.Length)
        {
            var index = haystack.IndexOf(term, searchFrom, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return false;
            var beforeIsWord = index > 0 && char.IsLetterOrDigit(haystack[index - 1]);
            var end = index + term.Length;
            var afterIsWord = end < haystack.Length && char.IsLetterOrDigit(haystack[end]);
            if (!beforeIsWord && !afterIsWord) return true;
            searchFrom = index + 1;
        }
        return false;
    }
}
