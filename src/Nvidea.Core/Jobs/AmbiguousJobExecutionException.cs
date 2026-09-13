namespace Nvidea.Core.Jobs;

/// <summary>
/// Signals that a side effect may already have happened but its intended postcondition
/// was not verified. The orchestrator must not retry this condition automatically.
/// </summary>
internal sealed class AmbiguousJobExecutionException : Exception
{
    public AmbiguousJobExecutionException(string message)
        : base(message)
    {
    }
}
