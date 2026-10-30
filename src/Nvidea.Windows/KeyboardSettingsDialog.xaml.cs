using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Nvidea.Windows;

public partial class KeyboardSettingsDialog : Window
{
    private readonly Dictionary<string, Button> _shortcutButtons;
    private string? _capturingAction;
    private string? _capturedPreviousValue;

    public KeyboardSettingsDialog(KeyboardShortcutSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        InitializeComponent();
        _shortcutButtons = new Dictionary<string, Button>(StringComparer.Ordinal)
        {
            ["Invoke"] = InvokeShortcutButton,
            ["LocalDictation"] = LocalDictationShortcutButton,
            ["SmartDictation"] = SmartDictationShortcutButton,
            ["Jarvis"] = JarvisShortcutButton,
            ["EmergencyStop"] = EmergencyStopShortcutButton,
            ["OpenSettings"] = OpenSettingsShortcutButton
        };
        SetButtons(settings);
    }

    public KeyboardShortcutSettings? ResultSettings { get; private set; }

    private void CaptureShortcutButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string action)
            return;
        RestoreCapturedButton();
        _capturingAction = action;
        _capturedPreviousValue = button.Content?.ToString();
        ShortcutStatusText.Text = "Press the desired combination. Use at least one modifier key. Press Esc to keep the current shortcut.";
        button.Content = "Press keys…";
        button.Focus();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_capturingAction is null)
            return;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            RestoreCapturedButton();
            ShortcutStatusText.Text = "Shortcut capture cancelled.";
            e.Handled = true;
            return;
        }

        if (!ShortcutChord.TryCreate(key, Keyboard.Modifiers, out var chord))
        {
            ShortcutStatusText.Text = "That key cannot be used alone. Press a supported key together with Ctrl, Alt, Shift, or Win.";
            e.Handled = true;
            return;
        }

        _shortcutButtons[_capturingAction].Content = chord.Display;
        _capturingAction = null;
        ShortcutStatusText.Text = "Shortcut captured. Save to apply it.";
        e.Handled = true;
    }

    private void RestoreDefaultsButton_Click(object sender, RoutedEventArgs e)
    {
        _capturingAction = null;
        SetButtons(KeyboardShortcutSettings.Defaults());
        ShortcutStatusText.Text = "Built-in shortcuts restored. Save to apply them.";
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = new KeyboardShortcutSettings
        {
            Invoke = ButtonValue("Invoke"),
            LocalDictation = ButtonValue("LocalDictation"),
            SmartDictation = ButtonValue("SmartDictation"),
            Jarvis = ButtonValue("Jarvis"),
            EmergencyStop = ButtonValue("EmergencyStop"),
            OpenSettings = ButtonValue("OpenSettings")
        };
        if (!settings.TryValidate(out var error))
        {
            ShortcutStatusText.Text = error;
            return;
        }

        ResultSettings = settings;
        DialogResult = true;
    }

    private string ButtonValue(string action) => _shortcutButtons[action].Content?.ToString() ?? string.Empty;

    private void SetButtons(KeyboardShortcutSettings settings)
    {
        foreach (var (action, value) in new[]
        {
            ("Invoke", settings.Invoke), ("LocalDictation", settings.LocalDictation),
            ("SmartDictation", settings.SmartDictation), ("Jarvis", settings.Jarvis),
            ("EmergencyStop", settings.EmergencyStop), ("OpenSettings", settings.OpenSettings)
        })
        {
            _shortcutButtons[action].Content = ShortcutChord.TryParse(value, out var chord) ? chord.Display : value;
        }
    }

    private void RestoreCapturedButton()
    {
        if (_capturingAction is { } action)
            _shortcutButtons[action].Content = _capturedPreviousValue;
        _capturingAction = null;
        _capturedPreviousValue = null;
    }
}
