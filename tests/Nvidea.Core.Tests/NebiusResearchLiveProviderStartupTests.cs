using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchLiveProviderStartupTests
{
    [Fact]
    public void CreateAfterDestinationPreflight_DoesNotInvokeFactory_WhenDestinationsAlias()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var sharedPath = Path.Combine(root, "evidence.json");
            var configuration = CreateConfiguration(sharedPath, sharedPath);
            var factoryInvocations = 0;

            var exception = Assert.Throws<InvalidOperationException>(() =>
                NebiusResearchLiveProviderStartup.CreateAfterDestinationPreflight(
                    configuration,
                    () =>
                    {
                        factoryInvocations++;
                        return new object();
                    }));

            Assert.Contains("must be different files", exception.Message, StringComparison.Ordinal);
            Assert.Equal(0, factoryInvocations);
            Assert.False(File.Exists(sharedPath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CreateAfterDestinationPreflight_InvokesFactoryExactlyOnce_AfterWritableDestinationsPass()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var manifestPath = Path.Combine(root, "manifest.json");
            var passPath = Path.Combine(root, "pass.json");
            var configuration = CreateConfiguration(manifestPath, passPath);
            var factoryInvocations = 0;

            var sentinel = new object();
            var result = NebiusResearchLiveProviderStartup.CreateAfterDestinationPreflight(
                configuration,
                () =>
                {
                    factoryInvocations++;
                    Assert.False(File.Exists(manifestPath));
                    Assert.False(File.Exists(passPath));
                    return sentinel;
                });

            Assert.Same(sentinel, result);
            Assert.Equal(1, factoryInvocations);
            Assert.False(File.Exists(manifestPath));
            Assert.False(File.Exists(passPath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static NebiusResearchLiveConfiguration CreateConfiguration(
        string? manifestPath,
        string? passPath) =>
        new(
            ServerlessAccessToken: null!,
            ProjectId: null!,
            ClientPrivateKeyPem: null!,
            DispatchOptions: null!,
            ObjectStorageOptions: null!,
            Report: null!,
            RedactedManifestPath: manifestPath,
            PassEvidencePath: passPath,
            PollSeconds: 1,
            TotalTimeoutMinutes: 1,
            ResearchQuestion: "test");

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-provider-startup-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
