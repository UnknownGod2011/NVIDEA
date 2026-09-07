using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class BrowserProfileOwnershipTests
{
    [Fact]
    public void PrepareOwnedProfileCreatesAndReusesDedicatedProfile()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var first = BrowserProfileOwnership.PrepareOwnedProfile(root);
            var second = BrowserProfileOwnership.PrepareOwnedProfile(root);

            Assert.Equal(first, second);
            Assert.Equal(
                Path.GetFullPath(Path.Combine(root, BrowserProfileOwnership.ProfileDirectoryName)),
                first);
            Assert.True(File.Exists(Path.Combine(first, BrowserProfileOwnership.MarkerFileName)));
            BrowserProfileOwnership.ValidateOwnedProfile(root, first);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PrepareOwnedProfileRefusesToAdoptUnmarkedDirectory()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var profile = Path.Combine(root, BrowserProfileOwnership.ProfileDirectoryName);
            Directory.CreateDirectory(profile);
            var sentinel = Path.Combine(profile, "existing-browser-state.txt");
            File.WriteAllText(sentinel, "do-not-touch");

            var error = Assert.Throws<InvalidOperationException>(() =>
                BrowserProfileOwnership.PrepareOwnedProfile(root));

            Assert.Contains("not marked as NVIDEA-owned", error.Message, StringComparison.Ordinal);
            Assert.Equal("do-not-touch", File.ReadAllText(sentinel));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PrepareOwnedProfileRejectsCorruptOwnershipMarkerWithoutDeletingState()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var profile = BrowserProfileOwnership.PrepareOwnedProfile(root);
            var marker = Path.Combine(profile, BrowserProfileOwnership.MarkerFileName);
            var sentinel = Path.Combine(profile, "Cookies");
            File.WriteAllText(sentinel, "session-state");
            File.WriteAllText(marker, "{ definitely-not-json }");

            Assert.Throws<InvalidOperationException>(() =>
                BrowserProfileOwnership.PrepareOwnedProfile(root));

            Assert.Equal("session-state", File.ReadAllText(sentinel));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ValidateOwnedProfileRejectsPathOutsideStateDirectory()
    {
        var root = CreateTemporaryDirectory();
        var outside = CreateTemporaryDirectory();
        try
        {
            var error = Assert.Throws<InvalidOperationException>(() =>
                BrowserProfileOwnership.ValidateOwnedProfile(root, outside));

            Assert.Contains("inside the NVIDEA state directory", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(outside, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-profile-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
