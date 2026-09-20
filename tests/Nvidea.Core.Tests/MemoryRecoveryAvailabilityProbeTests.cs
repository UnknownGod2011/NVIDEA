using Nvidea.Core.Memory;

namespace Nvidea.Core.Tests;

public sealed class MemoryRecoveryAvailabilityProbeTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"nvidea-memory-recovery-probe-{Guid.NewGuid():N}");

    [Fact]
    public async Task IsRecoveryOfferAllowedAsync_CorruptPrimaryWithBackup_AllowsExplicitOffer()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "memory.json");
        await File.WriteAllTextAsync(path, "[{\"broken\"");
        await File.WriteAllTextAsync($"{path}.bak", "[]");

        var allowed = await MemoryRecoveryAvailabilityProbe.IsRecoveryOfferAllowedAsync(path);

        Assert.True(allowed);
    }

    [Fact]
    public async Task IsRecoveryOfferAllowedAsync_HealthyPrimaryWithBackup_DoesNotAuthorizeRollback()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "memory.json");
        await File.WriteAllTextAsync(path, "[]");
        await File.WriteAllTextAsync($"{path}.bak", "[]");

        var allowed = await MemoryRecoveryAvailabilityProbe.IsRecoveryOfferAllowedAsync(path);

        Assert.False(allowed);
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
