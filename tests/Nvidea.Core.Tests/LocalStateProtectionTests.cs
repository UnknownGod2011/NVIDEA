using System.Text;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Nvidea.Core.Memory;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class LocalStateProtectionTests
{
    [Fact]
    public void Envelope_RoundTripsAndBindsPurpose()
    {
        var protector = new TestProtector();
        var plaintext = Encoding.UTF8.GetBytes("private payload");

        var encoded = LocalStateEnvelope.Encode(plaintext, protector, "memory");
        var decoded = LocalStateEnvelope.Decode(encoded, protector, "memory");

        Assert.True(decoded.WasProtected);
        Assert.Equal(plaintext, decoded.Plaintext);
        Assert.True(LocalStateEnvelope.HasProtectedHeader(encoded));
        Assert.Throws<InvalidOperationException>(() =>
            LocalStateEnvelope.Decode(encoded, protector, "jobs"));
    }

    [Fact]
    public async Task MemoryStore_ProtectsContentAndRoundTrips()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "memory.json");
            var protector = new TestProtector();
            using var store = new JsonFileMemoryStore(path, protector);
            var now = DateTimeOffset.UtcNow;
            var record = new MemoryRecord
            {
                Id = "m1",
                Layer = MemoryLayer.Semantic,
                Key = "private-note",
                Content = "ultra-secret-memory",
                Provenance = new MemoryProvenance("test"),
                CreatedAt = now,
                UpdatedAt = now,
                LastAccessedAt = now
            };

            await store.WriteAllAsync(new[] { record });

            var persisted = await File.ReadAllTextAsync(path);
            Assert.StartsWith("NVIDEA-STATE-V1", persisted, StringComparison.Ordinal);
            Assert.DoesNotContain("ultra-secret-memory", persisted, StringComparison.Ordinal);

            var restored = await store.ReadAllAsync();
            Assert.Single(restored);
            Assert.Equal("ultra-secret-memory", restored[0].Content);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("memory.json", "memory")]
    [InlineData("jobs.json", "jobs")]
    [InlineData("goal-sessions.json", "goals")]
    public async Task LegacyPlaintextStore_IsMigratedToProtectedEnvelope(string fileName, string kind)
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, fileName);
            await File.WriteAllTextAsync(path, "[]");
            var protector = new TestProtector();

            switch (kind)
            {
                case "memory":
                    using (var store = new JsonFileMemoryStore(path, protector))
                        Assert.Empty(await store.ReadAllAsync());
                    break;
                case "jobs":
                    Assert.Empty(await new JsonAgentJobStore(path, protector).ListAsync());
                    break;
                case "goals":
                    Assert.Empty(await new JsonBrowserGoalSessionStore(path, protector).ListAsync());
                    break;
                default:
                    throw new InvalidOperationException("Unknown test store kind.");
            }

            var migrated = await File.ReadAllTextAsync(path);
            Assert.StartsWith("NVIDEA-STATE-V1", migrated, StringComparison.Ordinal);
            Assert.DoesNotContain("[]", migrated, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void WindowsDpapiProtector_FailsClosedOutsideWindows()
    {
        if (OperatingSystem.IsWindows())
            return;

        var protector = new WindowsDpapiLocalStateProtector();
        Assert.Throws<PlatformNotSupportedException>(() =>
            protector.Protect(Encoding.UTF8.GetBytes("x"), "test"));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class TestProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose)
        {
            var prefix = Encoding.UTF8.GetBytes(purpose + "|");
            var result = new byte[prefix.Length + plaintext.Length];
            prefix.CopyTo(result, 0);
            plaintext.CopyTo(result.AsSpan(prefix.Length));
            return result;
        }

        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose)
        {
            var prefix = Encoding.UTF8.GetBytes(purpose + "|");
            if (!protectedData.StartsWith(prefix))
                throw new InvalidOperationException("Purpose mismatch.");
            return protectedData[prefix.Length..].ToArray();
        }
    }
}
