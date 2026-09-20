namespace Nvidea.Core.Memory;

/// <summary>
/// Determines whether the desktop may offer the explicit one-generation memory recovery choice.
/// The probe never reads the backup and never rewrites the primary: backup validation remains an operation
/// performed only after explicit user consent by <see cref="JsonFileMemoryStore.RecoverLastKnownGoodAsync"/>.
/// </summary>
public static class MemoryRecoveryAvailabilityProbe
{
    public static async Task<bool> IsRecoveryOfferAllowedAsync(
        string memoryPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(memoryPath))
            return false;

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(memoryPath);
        }
        catch
        {
            return false;
        }

        if (!File.Exists(fullPath) || !File.Exists($"{fullPath}.bak"))
            return false;

        using var store = new JsonFileMemoryStore(fullPath);
        try
        {
            // A healthy memory primary proves that a startup InvalidDataException originated elsewhere.
            // ValidatePrimaryAsync is intentionally non-mutating so merely deciding whether to show a
            // recovery prompt cannot migrate plaintext, rotate backups or otherwise alter durable state.
            await store.ValidatePrimaryAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }
        catch (InvalidDataException)
        {
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Permission, path, IO and other unexpected failures do not authorize destructive rollback.
            return false;
        }
    }
}
