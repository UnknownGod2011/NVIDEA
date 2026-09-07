using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class BrowserPostconditionTests
{
    [Fact]
    public void VerifyAll_RequiresEveryPredicateToMatch()
    {
        var observation = Observation(
            "https://example.com/success",
            "Submitted",
            "Application submitted successfully",
            new BrowserElement("e1", "checkbox", "Terms accepted", null, true, true, false, true));

        var result = BrowserPostconditionEvaluator.VerifyAll(
            new BrowserPostcondition[]
            {
                new(BrowserPostconditionKind.UrlEquals, "https://example.com/success"),
                new(BrowserPostconditionKind.TitleContains, "Submitted"),
                new(BrowserPostconditionKind.VisibleTextContains, "submitted successfully"),
                new(BrowserPostconditionKind.ElementCheckedEquals,
                    Locator: BrowserLocator.Accessibility("e1"), ExpectedBoolean: true)
            },
            observation);

        Assert.True(result.Verified);
    }

    [Fact]
    public async Task ConservativeVerifier_DoesNotAcceptPartialTypedPostconditions()
    {
        var before = Observation("https://example.com/form", "Form", "Ready");
        var after = Observation("https://example.com/success", "Submitted", "Application submitted");
        var action = new BrowserAction(
            BrowserActionKind.Click,
            BrowserLocator.ByRole("button", "Submit"),
            Postconditions: new BrowserPostcondition[]
            {
                new(BrowserPostconditionKind.UrlEquals, "https://example.com/success"),
                new(BrowserPostconditionKind.VisibleTextContains, "receipt #123")
            });

        var result = await new ConservativeBrowserVerifier().VerifyAsync(action, before, after);

        Assert.False(result.Verified);
    }

    [Fact]
    public void CrashReconciler_UsesSameTypedPredicateLanguage()
    {
        var observation = Observation("https://example.com/done", "Done", "Order complete");
        var action = new BrowserAction(
            BrowserActionKind.Click,
            BrowserLocator.ByRole("button", "Confirm"),
            Postconditions: new BrowserPostcondition[]
            {
                new(BrowserPostconditionKind.UrlEquals, "https://example.com/done"),
                new(BrowserPostconditionKind.VisibleTextContains, "Order complete")
            });

        var result = BrowserAmbiguousStateReconciler.TryVerify(action, observation);

        Assert.True(result.Verified);
    }

    [Fact]
    public void ElementValueEquals_IsExact_NotSubstringBased()
    {
        var observation = Observation(
            "https://example.com",
            "Page",
            "",
            new BrowserElement("e1", "textbox", "Status", "approved-pending", true, true, true));

        var result = BrowserPostconditionEvaluator.VerifyOne(
            new BrowserPostcondition(
                BrowserPostconditionKind.ElementValueEquals,
                Expected: "approved",
                Locator: BrowserLocator.Accessibility("e1")),
            observation);

        Assert.False(result.Verified);
    }

    [Fact]
    public void Upload_IsNeverAutoReconciled_EvenWhenTypedPredicateMatches()
    {
        var observation = Observation("https://example.com", "Uploaded", "Upload complete");
        var action = new BrowserAction(
            BrowserActionKind.Upload,
            BrowserLocator.ByRole("button", "Upload"),
            Value: "document.pdf",
            Postconditions: new BrowserPostcondition[]
            {
                new(BrowserPostconditionKind.VisibleTextContains, "Upload complete")
            });

        var result = BrowserAmbiguousStateReconciler.TryVerify(action, observation);

        Assert.False(result.Verified);
    }

    private static BrowserObservation Observation(
        string url,
        string title,
        string text,
        params BrowserElement[] elements) =>
        new(new Uri(url), title, elements, text, DateTimeOffset.UtcNow, SnapshotId: Guid.NewGuid().ToString("N"));
}
