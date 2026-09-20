using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class CompositionStartupFailureBoundaryTests
{
    [Fact]
    public void FailureAfterMemoryInitialization_DisposesEveryAcquiredResourceExactlyOnce()
    {
        AssertFailureBoundary(CompositionStartupCheckpoint.AfterMemoryInitialization,
            "memory", "embedding", "store", "nebius");
    }

    [Fact]
    public void FailureAfterTavilyAcquisition_DisposesEveryAcquiredResourceExactlyOnce()
    {
        AssertFailureBoundary(CompositionStartupCheckpoint.AfterTavilyAcquisition,
            "tavily", "memory", "embedding", "store", "nebius");
    }

    [Fact]
    public void FailureAfterCloudProviderTransfer_DisposesEveryAcquiredResourceExactlyOnce()
    {
        AssertFailureBoundary(CompositionStartupCheckpoint.AfterCloudProviderTransfer,
            "serverless", "object-storage", "tavily", "memory", "embedding", "store", "nebius");
    }

    [Fact]
    public void FailureBeforeOwnershipRelease_PreservesOriginalFailure_WhenCleanupThrows()
    {
        var original = new InvalidOperationException("authoritative startup failure");
        var cleanupOrder = new List<string>();

        var thrown = Assert.Throws<InvalidOperationException>(() =>
            CompositionStartupFailureBoundary.RunForTest(
                CompositionStartupCheckpoint.BeforeOwnershipRelease,
                original,
                cleanupOrder,
                cleanupFailureResource: "memory"));

        Assert.Same(original, thrown);
        Assert.Equal(
            new[] { "serverless", "object-storage", "tavily", "memory", "embedding", "store", "nebius" },
            cleanupOrder);
    }

    [Fact]
    public void Cancellation_RemainsCancellation_AndStillUnwindsAcquiredResources()
    {
        var cancellation = new OperationCanceledException("startup cancelled");
        var cleanupOrder = new List<string>();

        var thrown = Assert.Throws<OperationCanceledException>(() =>
            CompositionStartupFailureBoundary.RunForTest(
                CompositionStartupCheckpoint.AfterTavilyAcquisition,
                cancellation,
                cleanupOrder));

        Assert.Same(cancellation, thrown);
        Assert.Equal(new[] { "tavily", "memory", "embedding", "store", "nebius" }, cleanupOrder);
    }

    private static void AssertFailureBoundary(
        CompositionStartupCheckpoint checkpoint,
        params string[] expectedCleanupOrder)
    {
        var original = new InvalidOperationException("authoritative startup failure");
        var cleanupOrder = new List<string>();

        var thrown = Assert.Throws<InvalidOperationException>(() =>
            CompositionStartupFailureBoundary.RunForTest(checkpoint, original, cleanupOrder));

        Assert.Same(original, thrown);
        Assert.Equal(expectedCleanupOrder, cleanupOrder);
        Assert.Equal(expectedCleanupOrder.Length, cleanupOrder.Distinct(StringComparer.Ordinal).Count());
    }
}
