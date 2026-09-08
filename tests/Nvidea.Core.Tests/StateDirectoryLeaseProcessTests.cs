using System.Diagnostics;
using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class StateDirectoryLeaseProcessTests
{
    private const string ProbeStateDirectoryVariable = "NVIDEA_TEST_STATE_LEASE_PROBE_DIRECTORY";
    private const string ProbeReadyFileVariable = "NVIDEA_TEST_STATE_LEASE_PROBE_READY";
    private const string ProbeReleaseFileVariable = "NVIDEA_TEST_STATE_LEASE_PROBE_RELEASE";

    [Fact]
    public async Task IndependentProcessOwnsLeaseUntilCrashThenKernelReleasesIt()
    {
        var root = CreateTemporaryDirectory();
        var readyFile = Path.Combine(root, "probe.ready");
        var releaseFile = Path.Combine(root, "probe.release");
        Process? child = null;

        try
        {
            var projectPath = FindTestProject();
            var startInfo = new ProcessStartInfo
            {
                FileName = ResolveDotnetHost(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("test");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("--no-build");
            startInfo.ArgumentList.Add("--filter");
            startInfo.ArgumentList.Add($"FullyQualifiedName={typeof(StateDirectoryLeaseProcessTests).FullName}.{nameof(ChildHoldsLeaseUntilReleased)}");
            startInfo.Environment[ProbeStateDirectoryVariable] = root;
            startInfo.Environment[ProbeReadyFileVariable] = readyFile;
            startInfo.Environment[ProbeReleaseFileVariable] = releaseFile;

            child = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start the independent state-lease probe process.");

            await WaitForFileAsync(readyFile, child, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            var childLeaseInstanceId = (await File.ReadAllTextAsync(readyFile).ConfigureAwait(false)).Trim();
            Assert.False(string.IsNullOrWhiteSpace(childLeaseInstanceId));

            var contention = Assert.Throws<StateDirectoryLeaseUnavailableException>(() =>
                StateDirectoryLease.Acquire(root));
            Assert.NotNull(contention.Owner);
            Assert.Equal(childLeaseInstanceId, contention.Owner!.InstanceId);
            Assert.NotEqual(Environment.ProcessId, contention.Owner.ProcessId);

            // Simulate an ungraceful process death. The persistent lock file must remain harmless;
            // ownership is the live kernel lock, not file existence or cooperative cleanup.
            child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            using var reacquired = await AcquireEventuallyAsync(root, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            Assert.Equal(Environment.ProcessId, reacquired.Owner.ProcessId);
            Assert.True(File.Exists(Path.Combine(root, StateDirectoryLease.LeaseFileName)));
        }
        finally
        {
            if (child is { HasExited: false })
            {
                try
                {
                    File.WriteAllText(releaseFile, "release");
                    await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                }
                catch
                {
                    child.Kill(entireProcessTree: true);
                    await child.WaitForExitAsync().ConfigureAwait(false);
                }
            }

            child?.Dispose();
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ChildHoldsLeaseUntilReleased()
    {
        var stateDirectory = Environment.GetEnvironmentVariable(ProbeStateDirectoryVariable);
        var readyFile = Environment.GetEnvironmentVariable(ProbeReadyFileVariable);
        var releaseFile = Environment.GetEnvironmentVariable(ProbeReleaseFileVariable);

        // This fact doubles as a child-process probe. During an ordinary unfiltered test run it is
        // intentionally inert; the parent test starts a filtered independent testhost with all
        // three probe variables populated.
        if (string.IsNullOrWhiteSpace(stateDirectory)
            || string.IsNullOrWhiteSpace(readyFile)
            || string.IsNullOrWhiteSpace(releaseFile))
        {
            return;
        }

        using var lease = StateDirectoryLease.Acquire(stateDirectory);
        await File.WriteAllTextAsync(readyFile, lease.Owner.InstanceId).ConfigureAwait(false);

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);
        while (!File.Exists(releaseFile))
        {
            if (DateTimeOffset.UtcNow >= deadline)
                throw new TimeoutException("State-lease probe timed out waiting for release.");

            await Task.Delay(50).ConfigureAwait(false);
        }
    }

    private static async Task<StateDirectoryLease> AcquireEventuallyAsync(string stateDirectory, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (true)
        {
            try
            {
                return StateDirectoryLease.Acquire(stateDirectory);
            }
            catch (StateDirectoryLeaseUnavailableException) when (DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(50).ConfigureAwait(false);
            }
        }
    }

    private static async Task WaitForFileAsync(string path, Process child, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (!File.Exists(path))
        {
            if (child.HasExited)
            {
                var stdout = await child.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                var stderr = await child.StandardError.ReadToEndAsync().ConfigureAwait(false);
                throw new InvalidOperationException(
                    $"State-lease probe exited before becoming ready (exit {child.ExitCode}). stdout: {stdout} stderr: {stderr}");
            }

            if (DateTimeOffset.UtcNow >= deadline)
                throw new TimeoutException("Timed out waiting for the independent state-lease probe to become ready.");

            await Task.Delay(50).ConfigureAwait(false);
        }
    }

    private static string FindTestProject()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var direct = Path.Combine(directory.FullName, "Nvidea.Core.Tests.csproj");
            if (File.Exists(direct))
                return direct;

            var repositoryRelative = Path.Combine(directory.FullName, "tests", "Nvidea.Core.Tests", "Nvidea.Core.Tests.csproj");
            if (File.Exists(repositoryRelative))
                return repositoryRelative;
        }

        throw new FileNotFoundException("Could not locate tests/Nvidea.Core.Tests/Nvidea.Core.Tests.csproj from the test output directory.");
    }

    private static string ResolveDotnetHost()
    {
        var configured = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        return string.IsNullOrWhiteSpace(configured) ? "dotnet" : configured;
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-state-lease-process-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
