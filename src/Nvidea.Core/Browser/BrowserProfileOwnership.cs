using System.Text.Json;

namespace Nvidea.Core.Browser;

/// <summary>
/// Creates and validates the dedicated Chromium user-data directory owned by NVIDEA.
/// The browser host never accepts an arbitrary external Chrome/Edge profile path, which avoids
/// silently taking control of a user's primary browser profile or its broader credential surface.
/// </summary>
public static class BrowserProfileOwnership
{
    public const string ProfileDirectoryName = "browser-profile";
    public const string MarkerFileName = ".nvidea-profile.json";
    public const int CurrentFormatVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string PrepareOwnedProfile(string stateDirectory)
    {
        if (string.IsNullOrWhiteSpace(stateDirectory))
            throw new ArgumentException("State directory is required.", nameof(stateDirectory));

        var stateRoot = Path.GetFullPath(stateDirectory);
        Directory.CreateDirectory(stateRoot);

        var profileDirectory = Path.GetFullPath(Path.Combine(stateRoot, ProfileDirectoryName));
        EnsureChildPath(stateRoot, profileDirectory);
        var markerPath = Path.Combine(profileDirectory, MarkerFileName);

        if (Directory.Exists(profileDirectory))
        {
            if (!File.Exists(markerPath))
                throw new InvalidOperationException("Existing browser profile directory is not marked as NVIDEA-owned; refusing to adopt it.");

            ValidateMarker(markerPath);
            return profileDirectory;
        }

        Directory.CreateDirectory(profileDirectory);
        try
        {
            var marker = new BrowserProfileMarker(
                CurrentFormatVersion,
                Guid.NewGuid().ToString("N"),
                DateTimeOffset.UtcNow);
            WriteMarkerAtomically(markerPath, marker);
            return profileDirectory;
        }
        catch
        {
            // Only remove a directory that this call just created, and only when it is still empty.
            // Browser data is never deleted here.
            try
            {
                if (!Directory.EnumerateFileSystemEntries(profileDirectory).Any())
                    Directory.Delete(profileDirectory);
            }
            catch
            {
                // Preserve the original failure. A subsequent run will fail closed if an unmarked
                // directory remains instead of adopting unknown profile state.
            }
            throw;
        }
    }

    public static void ValidateOwnedProfile(string stateDirectory, string profileDirectory)
    {
        if (string.IsNullOrWhiteSpace(stateDirectory))
            throw new ArgumentException("State directory is required.", nameof(stateDirectory));
        if (string.IsNullOrWhiteSpace(profileDirectory))
            throw new ArgumentException("Profile directory is required.", nameof(profileDirectory));

        var stateRoot = Path.GetFullPath(stateDirectory);
        var profileRoot = Path.GetFullPath(profileDirectory);
        EnsureChildPath(stateRoot, profileRoot);
        ValidateMarker(Path.Combine(profileRoot, MarkerFileName));
    }

    private static void ValidateMarker(string markerPath)
    {
        if (!File.Exists(markerPath))
            throw new InvalidOperationException("NVIDEA browser profile ownership marker is missing.");

        BrowserProfileMarker? marker;
        try
        {
            marker = JsonSerializer.Deserialize<BrowserProfileMarker>(File.ReadAllText(markerPath), JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("NVIDEA browser profile ownership marker is invalid.", ex);
        }

        if (marker is null
            || marker.FormatVersion != CurrentFormatVersion
            || string.IsNullOrWhiteSpace(marker.ProfileId)
            || !Guid.TryParseExact(marker.ProfileId, "N", out _)
            || marker.CreatedAt == default)
        {
            throw new InvalidOperationException("NVIDEA browser profile ownership marker is invalid or unsupported.");
        }
    }

    private static void EnsureChildPath(string stateRoot, string candidate)
    {
        var normalizedRoot = Path.TrimEndingDirectorySeparator(stateRoot);
        var prefix = normalizedRoot + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Browser profile must remain inside the NVIDEA state directory.");
    }

    private static void WriteMarkerAtomically(string markerPath, BrowserProfileMarker marker)
    {
        var temporaryPath = markerPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(marker, JsonOptions));
            File.Move(temporaryPath, markerPath, overwrite: false);
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch { }
        }
    }

    private sealed record BrowserProfileMarker(int FormatVersion, string ProfileId, DateTimeOffset CreatedAt);
}
