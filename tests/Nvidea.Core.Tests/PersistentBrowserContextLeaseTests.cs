using System.Reflection;
using Microsoft.Playwright;
using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class PersistentBrowserContextLeaseTests
{
    [Fact]
    public async Task Direct_factory_call_fails_closed_when_state_is_already_leased()
    {
        var stateDirectory = CreateTempDirectory();
        try
        {
            using var owner = StateDirectoryLease.Acquire(stateDirectory);
            var playwright = DispatchProxy.Create<IPlaywright, ThrowingPlaywrightProxy>();

            var error = await Assert.ThrowsAsync<StateDirectoryLeaseUnavailableException>(() =>
                PersistentBrowserContextFactory.LaunchAsync(
                    playwright,
                    stateDirectory,
                    new Uri("https://example.com"),
                    DriverOptions(),
                    headless: true));

            Assert.Equal(owner.Owner.InstanceId, error.Owner?.InstanceId);
        }
        finally
        {
            DeleteTempDirectory(stateDirectory);
        }
    }

    [Fact]
    public async Task Startup_failure_releases_intrinsic_state_lease()
    {
        var stateDirectory = CreateTempDirectory();
        try
        {
            var playwright = DispatchProxy.Create<IPlaywright, ThrowingPlaywrightProxy>();

            await Assert.ThrowsAnyAsync<Exception>(() =>
                PersistentBrowserContextFactory.LaunchAsync(
                    playwright,
                    stateDirectory,
                    new Uri("https://example.com"),
                    DriverOptions(),
                    headless: true));

            using var reacquired = StateDirectoryLease.Acquire(stateDirectory);
            Assert.Equal(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(stateDirectory)),
                Path.TrimEndingDirectorySeparator(reacquired.StateDirectory));
        }
        finally
        {
            DeleteTempDirectory(stateDirectory);
        }
    }

    private static PlaywrightBrowserDriverOptions DriverOptions() => new(
        AllowedHosts: null,
        MaxObservationCharacters: 12_000,
        MaxObservedElements: 250,
        ActionTimeoutMilliseconds: 15_000);

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-browser-lease-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort test cleanup only; never mask the behavioral assertion.
        }
    }

    public class ThrowingPlaywrightProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException(
                $"Playwright should not be reached before state ownership is established ({targetMethod?.Name ?? "unknown"}).");
    }
}
