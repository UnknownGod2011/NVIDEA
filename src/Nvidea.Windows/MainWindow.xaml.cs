using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

public partial class MainWindow : Window
{
    private const int HotKeyId = 0x4E56;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint VkSpace = 0x20;

    private readonly NvideaCompositionRoot _root;
    private HwndSource? _source;
    private DesktopContext? _pendingContext;
    private bool _running;

    public MainWindow(NvideaCompositionRoot root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;
        _root.Session.StatusChanged += Session_StatusChanged;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);

        if (!RegisterHotKey(handle, HotKeyId, ModControl | ModShift, VkSpace))
            StatusText.Text = "Global hotkey unavailable; use the Run button.";
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _root.Session.StatusChanged -= Session_StatusChanged;
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
            _ = UnregisterHotKey(handle, HotKeyId);
        _source?.RemoveHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WmHotKey = 0x0312;
        if (msg != WmHotKey || wParam.ToInt32() != HotKeyId)
            return IntPtr.Zero;

        // Capture before activation so context belongs to the app the user invoked NVIDEA from.
        _pendingContext = WindowsContextCapture.Capture(ClipboardCheck.IsChecked == true);
        UpdateContextLabel(_pendingContext);
        Show();
        Activate();
        PromptBox.Focus();
        handled = true;
        return IntPtr.Zero;
    }

    private async void InvokeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_running || string.IsNullOrWhiteSpace(PromptBox.Text))
            return;

        var allowClipboard = ClipboardCheck.IsChecked == true;
        var context = _pendingContext ?? WindowsContextCapture.Capture(allowClipboard);
        _pendingContext = null;

        // A hotkey capture may have happened before the user toggled clipboard disclosure.
        // Never reuse clipboard text unless disclosure is still explicitly enabled now.
        if (!allowClipboard)
            context = context with { ClipboardText = null };
        else if (string.IsNullOrWhiteSpace(context.ClipboardText))
            context = context with { ClipboardText = WindowsContextCapture.Capture(true).ClipboardText };

        UpdateContextLabel(context);
        var request = new DesktopInvocationRequest(
            PromptBox.Text.Trim(),
            context,
            ResolveMode(),
            allowClipboard);

        SetRunning(true);
        OutputBox.Text = string.Empty;
        try
        {
            var result = await _root.Session.InvokeAsync(request);
            OutputBox.Text = result.Answer;
        }
        catch (OperationCanceledException)
        {
            OutputBox.Text = "Stopped.";
        }
        catch (Exception ex)
        {
            OutputBox.Text = $"NVIDEA could not complete this request.\n\n{ex.Message}";
        }
        finally
        {
            SetRunning(false);
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _ = _root.Session.EmergencyStop();
    }

    private void Session_StatusChanged(object? sender, DesktopAgentStatus status)
    {
        Dispatcher.InvokeAsync(() =>
        {
            StatusText.Text = string.IsNullOrWhiteSpace(status.Detail)
                ? status.State.ToString()
                : $"{status.State} — {status.Detail}";
        });
    }

    private DesktopInvocationMode ResolveMode() => ModeBox.SelectedIndex switch
    {
        1 => DesktopInvocationMode.Chat,
        2 => DesktopInvocationMode.Research,
        _ => DesktopInvocationMode.Auto
    };

    private void SetRunning(bool running)
    {
        _running = running;
        InvokeButton.IsEnabled = !running;
        PromptBox.IsEnabled = !running;
        ModeBox.IsEnabled = !running;
        ClipboardCheck.IsEnabled = !running;
        StopButton.IsEnabled = running;
    }

    private void UpdateContextLabel(DesktopContext context)
    {
        var app = string.IsNullOrWhiteSpace(context.ActiveApplication) ? "unknown app" : context.ActiveApplication;
        var title = string.IsNullOrWhiteSpace(context.WindowTitle) ? string.Empty : $" · {context.WindowTitle}";
        ContextText.Text = $"Context: {app}{title}";
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
