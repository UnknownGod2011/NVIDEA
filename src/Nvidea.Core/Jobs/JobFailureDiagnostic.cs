using System.Text;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Converts untrusted handler/provider exception text into bounded local diagnostic evidence.
/// The NVIDEA-owned prefix intentionally prevents arbitrary exception messages from being mistaken
/// for any structured or legacy provider-failure evidence format.
/// </summary>
internal static class JobFailureDiagnostic
{
    internal const int MaxDetailLength = 768;
    internal const string Prefix = "Execution error (untrusted): ";
    internal const string Fallback = "Execution error (untrusted).";

    internal static string FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var message = exception.Message;
        if (string.IsNullOrWhiteSpace(message))
            return Fallback;

        var detail = new StringBuilder(Math.Min(message.Length, MaxDetailLength));
        var pendingSpace = false;

        foreach (var character in message)
        {
            if (char.IsControl(character) || char.IsWhiteSpace(character))
            {
                pendingSpace = detail.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                if (detail.Length >= MaxDetailLength)
                    break;
                detail.Append(' ');
                pendingSpace = false;
            }

            if (detail.Length >= MaxDetailLength)
                break;

            detail.Append(character);
        }

        if (detail.Length == 0)
            return Fallback;

        return Prefix + detail;
    }
}
