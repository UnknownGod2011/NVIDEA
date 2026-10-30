using System.Runtime.InteropServices;
using System.Windows;

namespace Nvidea.Windows;

/// <summary>
/// Restores the exact foreground window captured before voice UI activation, then pastes bounded text.
/// A focus mismatch fails closed instead of typing into a different application.
/// </summary>
internal sealed record WindowsFocusTarget(IntPtr Handle, uint ProcessId)
{
    public static WindowsFocusTarget? Capture()
    {
        var handle = GetForegroundWindow();
        if (handle == IntPtr.Zero) return null;
        _ = GetWindowThreadProcessId(handle, out var processId);
        return processId == 0 ? null : new WindowsFocusTarget(handle, processId);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}

internal static class WindowsTextInsertion
{
    public static async Task InsertIntoCapturedTargetAsync(WindowsFocusTarget target, string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text is required.", nameof(text));
        if (text.Length > 4_000) throw new ArgumentException("Text is too long for safe voice insertion.", nameof(text));
        cancellationToken.ThrowIfCancellationRequested();

        if (!SetForegroundWindow(target.Handle))
            throw new InvalidOperationException("Windows could not restore the application you invoked voice from; text was not inserted.");
        await Task.Delay(120, cancellationToken).ConfigureAwait(true);
        if (GetForegroundWindow() != target.Handle)
            throw new InvalidOperationException("The foreground application changed during voice capture; text was not inserted.");

        IDataObject? previous = null;
        try
        {
            previous = Clipboard.GetDataObject();
            Clipboard.SetText(text, TextDataFormat.UnicodeText);
            keybd_event(VkControl, 0, 0, UIntPtr.Zero);
            keybd_event(VkV, 0, 0, UIntPtr.Zero);
            keybd_event(VkV, 0, KeyEventKeyUp, UIntPtr.Zero);
            keybd_event(VkControl, 0, KeyEventKeyUp, UIntPtr.Zero);
        }
        finally
        {
            if (previous is not null)
            {
                try { Clipboard.SetDataObject(previous, true); }
                catch (COMException) { /* another application owns the clipboard; never retry injection */ }
            }
        }
    }

    private const byte VkControl = 0x11;
    private const byte VkV = 0x56;
    private const uint KeyEventKeyUp = 0x0002;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}
