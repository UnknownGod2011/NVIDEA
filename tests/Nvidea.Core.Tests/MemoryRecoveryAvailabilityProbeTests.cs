using System.Text;
using Nvidea.Core.Memory;

namespace Nvidea.Core.Tests;

public sealed class MemoryRecoveryAvailabilityProbeTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"nvidea-memory-recovery-probe-{Guid.NewGuid():N}");

    [Fact]
    public async Task IsRecoveryOfferAllowedAsync_CorruptPrimaryWithBackup_AllowsExplicitOfferWithoutTouchingEitherGeneration()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "memory.json");
        var primary = Encoding.UTF8.GetBytes("[{\"broken\"");
        var backup = Encoding.UTF8.GetBytes("[]");
        await File.WriteAllBytesAsync(path, primary);
        await File.WriteAllBytesAsync($"{path}.bak", backup);

        var allowed = await MemoryRecoveryAvailabilityProbe.IsRecoveryOfferAllowedAsync(path);

        Assert.True(allowed);
        Assert.Equal(primary, await File.ReadAllBytesAsync(path));
        Assert.Equal(backup, await File.ReadAllBytesAsync($"{path}.bak"));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public async Task IsRecoveryOfferAllowedAsync_HealthyPrimaryWithBackup_DoesNotAuthorizeOrMutateRollbackState()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "memory.json");
        var primary = Encoding.UTF8.GetBytes("[]");
        var backup = Encoding.UTF8.GetBytes("[{\"sentinel\":true}]");
        await File.WriteAllBytesAsync(path, primary);
        await File.WriteAllBytesAsync($"{path}.bak", backup);

        var allowed = await MemoryRecoveryAvailabilityProbe.IsRecoveryOfferAllowedAsync(path);

        Assert.False(allowed);
        Assert.Equal(primary, await File.ReadAllBytesAsync(path));
        Assert.Equal(backup, await File.ReadAllBytesAsync($"{path}.bak"));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task IsRecoveryOfferAllowedAsync_MissingRequiredGeneration_DoesNotAuthorizeRollback(
        bool createPrimary,
        bool createBackup)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "memory.json");
        if (createPrimary)
            await File.WriteAllTextAsync(path, "[{\"broken\"");
        if (createBackup)
            await File.WriteAllTextAsync($"{path}.bak", "[]");

        var allowed = await MemoryRecoveryAvailabilityProbe.IsRecoveryOfferAllowedAsync(path);

        Assert.False(allowed);
    }

    [Fact]
    public async Task IsRecoveryOfferAllowedAsync_CancellationPropagatesWithoutAuthorizingRollback()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "memory.json");
        await File.WriteAllTextAsync(path, "[{\"broken\"");
        await File.WriteAllTextAsync($"{path}.bak", "[]");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => MemoryRecoveryAvailabilityProbe.IsRecoveryOfferAllowedAsync(path, cts.Token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsRecoveryOfferAllowedAsync_InvalidPath_DoesNotAuthorizeRollback(string path)
    {
        Assert.False(await MemoryRecoveryAvailabilityProbe.IsRecoveryOfferAllowedAsync(path));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
        catch
        {
            // Best-effort test cleanup only.
        }
    }
}
