using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class AtomicTextArtifactWriterTests
{
    [Fact]
    public void Write_CreatesAndReplacesDestinationWithoutLeavingTemporaryFiles()
    {
        var root = CreateRoot();
        try
        {
            var path = Path.Combine(root, "artifact.json");

            AtomicTextArtifactWriter.Write(path, "first");
            Assert.Equal("first", File.ReadAllText(path));

            AtomicTextArtifactWriter.Write(path, "second");
            Assert.Equal("second", File.ReadAllText(path));
            Assert.Empty(Directory.GetFiles(root, ".artifact.json.*.tmp"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ValidateDestination_RejectsDirectoryTargets()
    {
        var root = CreateRoot();
        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                AtomicTextArtifactWriter.ValidateDestination(root, "Evidence"));

            Assert.DoesNotContain(root, exception.Message, StringComparison.Ordinal);
            Assert.Contains("must not target a directory", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ValidateWritableDestination_ProbesSiblingWithoutCreatingFinalArtifact()
    {
        var root = CreateRoot();
        try
        {
            var path = Path.Combine(root, "manifest.json");

            var validated = AtomicTextArtifactWriter.ValidateWritableDestination(path, "Manifest");

            Assert.Equal(Path.GetFullPath(path), validated);
            Assert.False(File.Exists(path));
            Assert.Empty(Directory.GetFiles(root, ".manifest.json.*.probe.tmp"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ValidateWritableDestination_PreservesExistingFinalArtifact()
    {
        var root = CreateRoot();
        try
        {
            var path = Path.Combine(root, "pass.json");
            File.WriteAllText(path, "existing-evidence");

            AtomicTextArtifactWriter.ValidateWritableDestination(path, "PASS evidence");

            Assert.Equal("existing-evidence", File.ReadAllText(path));
            Assert.Empty(Directory.GetFiles(root, ".pass.json.*.probe.tmp"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ValidateWritableDestination_RejectsMissingParentWithoutCreatingDirectories()
    {
        var root = CreateRoot();
        try
        {
            var missing = Path.Combine(root, "missing");
            var path = Path.Combine(missing, "pass.json");

            Assert.Throws<InvalidOperationException>(() =>
                AtomicTextArtifactWriter.ValidateWritableDestination(path, "PASS evidence"));

            Assert.False(Directory.Exists(missing));
            Assert.False(File.Exists(path));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nvidea-atomic-writer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
