using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class StateDirectoryLeaseTests
{
    [Fact]
    public void AcquireIsExclusiveForSameStateDirectory()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            using var first = StateDirectoryLease.Acquire(root);

            var error = Assert.Throws<StateDirectoryLeaseUnavailableException>(() =>
                StateDirectoryLease.Acquire(root));

            Assert.Contains("owns this durable state directory", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(error.Owner);
            Assert.Equal(Environment.ProcessId, error.Owner!.ProcessId);
            Assert.Equal(first.Owner.InstanceId, error.Owner.InstanceId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DisposeReleasesKernelLockAndAllowsReacquisition()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            string firstInstance;
            using (var first = StateDirectoryLease.Acquire(root))
                firstInstance = first.Owner.InstanceId;

            using var second = StateDirectoryLease.Acquire(root);

            Assert.NotEqual(firstInstance, second.Owner.InstanceId);
            Assert.Equal(Environment.ProcessId, second.Owner.ProcessId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void StaleLeaseFileWithoutKernelOwnerDoesNotBlockStartup()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var leasePath = Path.Combine(root, StateDirectoryLease.LeaseFileName);
            File.WriteAllText(leasePath, "{\"formatVersion\":1,\"instanceId\":\"00000000000000000000000000000000\",\"processId\":999999,\"acquiredAt\":\"2026-01-01T00:00:00Z\"}");

            using var lease = StateDirectoryLease.Acquire(root);

            Assert.Equal(Environment.ProcessId, lease.Owner.ProcessId);
            Assert.NotEqual("00000000000000000000000000000000", lease.Owner.InstanceId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DifferentStateDirectoriesDoNotContend()
    {
        var firstRoot = CreateTemporaryDirectory();
        var secondRoot = CreateTemporaryDirectory();
        try
        {
            using var first = StateDirectoryLease.Acquire(firstRoot);
            using var second = StateDirectoryLease.Acquire(secondRoot);

            Assert.NotEqual(first.LeasePath, second.LeasePath);
            Assert.NotEqual(first.Owner.InstanceId, second.Owner.InstanceId);
        }
        finally
        {
            Directory.Delete(firstRoot, recursive: true);
            Directory.Delete(secondRoot, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-state-lease-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
