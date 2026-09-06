using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

internal static class WindowsContextCapture
{
    public static DesktopContext Capture(bool includeClipboard)
    {
        var hwnd = GetForegroundWindow();
        string? title = null;
        string? application = null;

        if (hwnd != IntPtr.Zero)
        {
            var length = GetWindowTextLength(hwnd);
            if (length > 0)
            {
                var builder = new StringBuilder(Math.Min(length + 1, 1024));
                _ = GetWindowText(hwnd, builder, builder.Capacity);
                title = Bound(builder.ToString(), 512);
            }

            _ = GetWindowThreadProcessId(hwnd, out var processId);
            if (processId != 0)
            {
                try
                {
                    using var process = Process.GetProcessById(unchecked((int)processId));
                    application = Bound(process.ProcessName, 256);
                }
                catch (ArgumentException)
                {
                    // The foreground process exited between inspection calls.
                }
                catch (InvalidOperationException)
                {
                    // Process metadata became unavailable; omit it rather than failing invocation.
                }
            }
        }

        string? clipboard = null;
        if (includeClipboard)
        {
            try
            {
                if (Clipboard.ContainsText())
                    clipboard = Bound(Clipboard.GetText(TextDataFormat.UnicodeText), 8_000);
            }
            catch (COMException)
            {
                // Clipboard can be temporarily locked by another process. Treat it as unavailable.
            }
        }

        return new DesktopContext(
            ActiveApplication: application,
            SelectedText: null,
            ClipboardText: clipboard,
            WindowTitle: title);
    }

    private static string? Bound(string? value, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxChars ? trimmed : trimmed[..maxChars] + "…";
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
