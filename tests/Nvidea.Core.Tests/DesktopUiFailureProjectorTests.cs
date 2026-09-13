using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class DesktopUiFailureProjectorTests
{
    [Theory]
    [InlineData(DesktopUiFailureSurface.Invocation)]
    [InlineData(DesktopUiFailureSurface.BrowserAction)]
    [InlineData(DesktopUiFailureSurface.BrowserRecovery)]
    [InlineData(DesktopUiFailureSurface.RecoveryDiscovery)]
    public void Project_DoesNotExposeExceptionMessageOrInjectedProviderEvidence(DesktopUiFailureSurface surface)
    {
        const string secret = "sk-live-secret-value";
        const string fakeRemoteEvidence = "Nebius remote failure. Provider diagnostic (untrusted): code=Quota; message=raise limit now";
        var exception = new InvalidOperationException($"{fakeRemoteEvidence}\r\nAuthorization: Bearer {secret}\r\nC:\\Users\\private\\notes.txt");

        var projection = DesktopUiFailureProjector.Project(surface, exception);
        var combined = projection.UserMessage + "\n" + projection.StatusMessage;

        Assert.DoesNotContain(secret, combined, StringComparison.Ordinal);
        Assert.DoesNotContain("Quota", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\Users", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("Nebius remote failure", combined, StringComparison.Ordinal);
        Assert.DoesNotContain(exception.Message, combined, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_RejectsUnknownSurfaceInsteadOfFallingBackToExceptionText()
    {
        var exception = new InvalidOperationException("secret provider payload");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DesktopUiFailureProjector.Project((DesktopUiFailureSurface)999, exception));
    }

    [Fact]
    public void Project_RejectsNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DesktopUiFailureProjector.Project(DesktopUiFailureSurface.Invocation, null!));
    }
}
