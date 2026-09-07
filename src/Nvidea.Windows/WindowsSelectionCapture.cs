using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

namespace Nvidea.Windows;

/// <summary>
/// Reads the current text selection through Windows UI Automation without synthesizing
/// keystrokes or mutating the clipboard. Unsupported/inaccessible controls fail closed.
/// </summary>
internal static class WindowsSelectionCapture
{
    private const int MaxSelectionChars = 8_000;
    private const int MaxRanges = 16;

    public static string? TryCapture()
    {
        try
        {
            var focused = AutomationElement.FocusedElement;
            if (focused is null)
                return null;

            if (!focused.TryGetCurrentPattern(TextPattern.Pattern, out var rawPattern) || rawPattern is not TextPattern pattern)
                return null;

            if (pattern.SupportedTextSelection == SupportedTextSelection.None)
                return null;

            var ranges = pattern.GetSelection();
            if (ranges is null || ranges.Length == 0)
                return null;

            var builder = new StringBuilder(Math.Min(MaxSelectionChars, 1_024));
            foreach (var range in ranges.Take(MaxRanges))
            {
                if (builder.Length >= MaxSelectionChars)
                    break;

                // GetSelection may return a degenerate insertion-point range when there is no
                // actual selection; GetText then returns an empty string, which we ignore.
                var remaining = MaxSelectionChars - builder.Length;
                var text = range.GetText(remaining);
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (builder.Length > 0)
                {
                    if (builder.Length + Environment.NewLine.Length >= MaxSelectionChars)
                        break;
                    builder.AppendLine();
                }

                remaining = MaxSelectionChars - builder.Length;
                var normalized = text.Trim();
                builder.Append(normalized.Length <= remaining ? normalized : normalized[..remaining]);
            }

            if (builder.Length == 0)
                return null;

            return builder.ToString();
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
    }
}
