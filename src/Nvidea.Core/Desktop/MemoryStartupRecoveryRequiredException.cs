namespace Nvidea.Core.Desktop;

/// <summary>
/// Signals that durable personal-memory state could not be opened during desktop startup.
/// The exception intentionally carries no persisted payload, path, protector details, or provider
/// exception text so UI code can offer a bounded recovery choice without disclosing memory data.
/// </summary>
public sealed class MemoryStartupRecoveryRequiredException : InvalidDataException
{
    public MemoryStartupRecoveryRequiredException()
        : base("Durable personal memory could not be opened safely.")
    {
    }

    public MemoryStartupRecoveryRequiredException(Exception innerException)
        : base("Durable personal memory could not be opened safely.", innerException)
    {
    }
}
