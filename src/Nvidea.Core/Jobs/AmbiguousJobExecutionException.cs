namespace Nvidea.Core.Jobs;

/// <summary>
/// Signals that a side effect may already have happened but its intended postcondition
/// was not verified. Job handlers use this contract to force fail-closed recovery instead
/// of automatic retry when replay could duplicate a consequential action.
/// </summary>
public sealed class AmbiguousJobExecutionException : Exception
{
    public AmbiguousJobExecutionException(string message)
        : base(message)
    {
    }
}
