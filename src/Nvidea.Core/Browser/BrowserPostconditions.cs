namespace Nvidea.Core.Browser;

public enum BrowserPostconditionKind
{
    UrlEquals,
    TitleContains,
    VisibleTextContains,
    ElementExists,
    ElementValueEquals,
    ElementCheckedEquals,
    ElementEnabledEquals
}

public sealed record BrowserPostcondition(
    BrowserPostconditionKind Kind,
    string? Expected = null,
    BrowserLocator? Locator = null,
    bool? ExpectedBoolean = null);

public static class BrowserPostconditionEvaluator
{
    public static (bool Verified, string Detail) VerifyAll(
        IReadOnlyList<BrowserPostcondition> postconditions,
        BrowserObservation observation)
    {
        ArgumentNullException.ThrowIfNull(postconditions);
        ArgumentNullException.ThrowIfNull(observation);

        if (postconditions.Count == 0)
            return (false, "No typed browser postconditions were supplied.");
        if (postconditions.Count > 8)
            return (false, "Browser action declared more than 8 postconditions.");

        foreach (var postcondition in postconditions)
        {
            var result = VerifyOne(postcondition, observation);
            if (!result.Verified)
                return result;
        }

        return (true, $"Verified {postconditions.Count} typed browser postcondition(s).");
    }

    public static (bool Verified, string Detail) VerifyOne(
        BrowserPostcondition postcondition,
        BrowserObservation observation)
    {
        ArgumentNullException.ThrowIfNull(postcondition);
        ArgumentNullException.ThrowIfNull(observation);

        switch (postcondition.Kind)
        {
            case BrowserPostconditionKind.UrlEquals:
                if (!TryAbsoluteHttpUri(postcondition.Expected, out var expectedUri))
                    return (false, "URL postcondition is invalid.");
                var expected = Normalize(expectedUri);
                var actual = Normalize(observation.Url);
                return expected == actual
                    ? (true, "Exact URL postcondition verified.")
                    : (false, $"Expected URL {expected}; observed {actual}.");

            case BrowserPostconditionKind.TitleContains:
                return Contains(postcondition.Expected, observation.Title, "title");

            case BrowserPostconditionKind.VisibleTextContains:
                return Contains(postcondition.Expected, observation.VisibleText, "visible text");

            case BrowserPostconditionKind.ElementExists:
            {
                var element = FindElement(postcondition.Locator, observation);
                return element is not null
                    ? (true, "Element existence postcondition verified.")
                    : (false, "Expected element was not present in the fresh observation.");
            }

            case BrowserPostconditionKind.ElementValueEquals:
            {
                var element = FindElement(postcondition.Locator, observation);
                if (element is null)
                    return (false, "Expected element was not present in the fresh observation.");
                if (postcondition.Expected is null)
                    return (false, "Element value postcondition is missing its expected value.");
                var matches = string.Equals(element.Value ?? string.Empty, postcondition.Expected, StringComparison.Ordinal);
                return matches
                    ? (true, "Element value postcondition verified.")
                    : (false, "Observed element value did not exactly match the expected value.");
            }

            case BrowserPostconditionKind.ElementCheckedEquals:
            case BrowserPostconditionKind.ElementEnabledEquals:
            {
                var element = FindElement(postcondition.Locator, observation);
                if (element is null)
                    return (false, "Expected element was not present in the fresh observation.");
                if (postcondition.ExpectedBoolean is null)
                    return (false, "Boolean element postcondition is missing its expected value.");
                var actual = postcondition.Kind == BrowserPostconditionKind.ElementCheckedEquals
                    ? element.IsChecked
                    : element.IsEnabled;
                return actual == postcondition.ExpectedBoolean.Value
                    ? (true, $"Element {postcondition.Kind} postcondition verified.")
                    : (false, $"Element {postcondition.Kind} postcondition did not match fresh browser state.");
            }

            default:
                return (false, "Unsupported browser postcondition kind.");
        }
    }

    private static (bool Verified, string Detail) Contains(string? expected, string actual, string field)
    {
        if (string.IsNullOrWhiteSpace(expected))
            return (false, $"{field} postcondition is missing its expected text.");
        var verified = actual.Contains(expected.Trim(), StringComparison.OrdinalIgnoreCase);
        return verified
            ? (true, $"Expected {field} text was observed.")
            : (false, $"Expected {field} text was not observed.");
    }

    private static BrowserElement? FindElement(BrowserLocator? locator, BrowserObservation observation)
    {
        if (locator is null)
            return null;

        return locator.Kind switch
        {
            BrowserLocatorKind.AccessibilityRef => observation.Elements.FirstOrDefault(e =>
                string.Equals(e.Reference, locator.Value, StringComparison.Ordinal)),
            BrowserLocatorKind.RoleAndName => observation.Elements.FirstOrDefault(e =>
                string.Equals(e.Role, locator.Role, StringComparison.OrdinalIgnoreCase)
                && string.Equals(e.Name, locator.Name ?? locator.Value, StringComparison.OrdinalIgnoreCase)),
            BrowserLocatorKind.Label or BrowserLocatorKind.Text => observation.Elements.FirstOrDefault(e =>
                string.Equals(e.Name, locator.Value, StringComparison.OrdinalIgnoreCase)),
            BrowserLocatorKind.TestId or BrowserLocatorKind.Css => null,
            _ => null
        };
    }

    private static bool TryAbsoluteHttpUri(string? value, out Uri uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var parsed)
            && parsed.Scheme is "http" or "https")
        {
            uri = parsed;
            return true;
        }

        uri = null!;
        return false;
    }

    private static string Normalize(Uri uri)
    {
        var builder = new UriBuilder(uri) { Fragment = string.Empty };
        return builder.Uri.AbsoluteUri.TrimEnd('/');
    }
}
