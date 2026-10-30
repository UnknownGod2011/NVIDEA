using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Nvidea.Core.Nebius;

namespace Nvidea.Windows;

public partial class KeyboardSettingsDialog : Window
{
    private readonly Dictionary<string, Button> _shortcutButtons;
    private readonly KeyboardWtfProviderSettingsSummary _providerSettingsSummary;
    private string? _capturingAction;
    private string? _capturedPreviousValue;

    public KeyboardSettingsDialog(
        KeyboardShortcutSettings settings,
        KeyboardWtfProviderSettingsSummary? providerSettingsSummary = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        InitializeComponent();
        _providerSettingsSummary = providerSettingsSummary ?? new KeyboardWtfProviderSettingsSummary(
            false,
            false,
            NebiusOptions.VerifiedNemotronNanoModel,
            NebiusOptions.VerifiedNemotronSuperModel,
            NebiusOptions.VerifiedNemotronUltraModel);
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
        InitializeProviderSettings(_providerSettingsSummary);
        InitializeVoiceSettings(_providerSettingsSummary);
        Closed += (_, _) => ClearSensitiveInputs();
    }

    public KeyboardShortcutSettings? ResultSettings { get; private set; }
    public KeyboardWtfProviderSettingsEdit? ProviderSettingsEdit { get; private set; }

    private void InitializeProviderSettings(KeyboardWtfProviderSettingsSummary summary)
    {
        FastModelTextBox.Text = summary.FastModel;
        StandardModelTextBox.Text = summary.StandardModel;
        DeepModelTextBox.Text = summary.DeepModel;
        NebiusKeyStatusText.Text = summary.HasSavedNebiusApiKey
            ? "A Nebius key is saved for this Windows user. Leave blank to keep it or enter a replacement."
            : "No Nebius key is saved. You can enter one here or configure NEBIUS_API_KEY in the environment.";
        TavilyKeyStatusText.Text = summary.HasSavedTavilyApiKey
            ? "A Tavily key is saved for this Windows user. Leave blank to keep it or enter a replacement."
            : "No Tavily key is saved. Research can also use TAVILY_API_KEY from the environment.";
        if (!summary.IsReadable)
        {
            ReplaceUnreadableSettingsCheckBox.Visibility = Visibility.Visible;
            ProviderSettingsStatusText.Text = summary.Notice ?? "Saved provider settings could not be read safely.";
        }
        else
        {
            ReplaceUnreadableSettingsCheckBox.Visibility = Visibility.Collapsed;
        }
    }

    private void NebiusTokenFactoryKeyLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        var uri = e.Uri;
        if (uri is null
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.Host, "tokenfactory.nebius.com", StringComparison.OrdinalIgnoreCase))
        {
            ProviderSettingsStatusText.Text = "The Token Factory link was rejected because its address was not trusted.";
            e.Handled = true;
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            ProviderSettingsStatusText.Text = "Windows could not open the Token Factory page. Open https://tokenfactory.nebius.com/ in your browser.";
        }

        e.Handled = true;
    }

    private void InitializeVoiceSettings(KeyboardWtfProviderSettingsSummary summary)
    {
        AssistantNameTextBox.Text = summary.AssistantName;
        SpeakJarvisResponsesCheckBox.IsChecked = summary.SpeakJarvisResponses;
        UsePersonalMemoryCheckBox.IsChecked = summary.UsePersonalMemory;

        foreach (var tone in new[] { "Balanced", "Concise", "Friendly", "Professional" })
            AddTaggedItem(AssistantToneComboBox, tone, tone);
        SelectTaggedItem(AssistantToneComboBox, summary.AssistantTone);

        AddTaggedItem(SpeechLanguageComboBox, "Automatic (Windows language)", "auto");
        var installedLanguages = SystemSpeechStreamingRecognizer.GetInstalledLanguages();
        foreach (var language in installedLanguages)
            AddTaggedItem(SpeechLanguageComboBox, language, language);
        if (!string.Equals(summary.SpeechLanguage, "auto", StringComparison.OrdinalIgnoreCase)
            && !installedLanguages.Contains(summary.SpeechLanguage, StringComparer.OrdinalIgnoreCase))
        {
            AddTaggedItem(SpeechLanguageComboBox, $"{summary.SpeechLanguage} (not installed)", summary.SpeechLanguage);
        }
        SelectTaggedItem(SpeechLanguageComboBox, summary.SpeechLanguage);

        AddTaggedItem(SpeechOutputVoiceComboBox, "System default", "System default");
        var installedVoices = LocalSpeechOutput.GetInstalledVoiceNames();
        foreach (var voice in installedVoices)
            AddTaggedItem(SpeechOutputVoiceComboBox, voice, voice);
        if (!string.Equals(summary.SpeechOutputVoice, "System default", StringComparison.OrdinalIgnoreCase)
            && !installedVoices.Contains(summary.SpeechOutputVoice, StringComparer.OrdinalIgnoreCase))
        {
            AddTaggedItem(SpeechOutputVoiceComboBox, $"{summary.SpeechOutputVoice} (not installed)", summary.SpeechOutputVoice);
        }
        SelectTaggedItem(SpeechOutputVoiceComboBox, summary.SpeechOutputVoice);

        if (!HasTaggedItem(MaximumListeningSecondsComboBox, summary.MaximumListeningSeconds.ToString()))
            AddTaggedItem(MaximumListeningSecondsComboBox, $"Custom ({summary.MaximumListeningSeconds} seconds)", summary.MaximumListeningSeconds.ToString());
        if (!HasTaggedItem(EndOfTurnSilenceComboBox, summary.EndOfTurnSilenceMilliseconds.ToString()))
            AddTaggedItem(EndOfTurnSilenceComboBox, $"Custom ({summary.EndOfTurnSilenceMilliseconds} ms)", summary.EndOfTurnSilenceMilliseconds.ToString());
        if (!HasTaggedItem(SpeechOutputRateComboBox, summary.SpeechOutputRate.ToString()))
            AddTaggedItem(SpeechOutputRateComboBox, $"Custom ({summary.SpeechOutputRate})", summary.SpeechOutputRate.ToString());
        SelectTaggedItem(MaximumListeningSecondsComboBox, summary.MaximumListeningSeconds.ToString());
        SelectTaggedItem(EndOfTurnSilenceComboBox, summary.EndOfTurnSilenceMilliseconds.ToString());
        SelectTaggedItem(SpeechOutputRateComboBox, summary.SpeechOutputRate.ToString());
    }

    private static void AddTaggedItem(ComboBox comboBox, string content, string tag) =>
        comboBox.Items.Add(new ComboBoxItem { Content = content, Tag = tag });

    private static void SelectTaggedItem(ComboBox comboBox, string? tag)
    {
        comboBox.SelectedItem = comboBox.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasTaggedItem(ComboBox comboBox, string tag) =>
        comboBox.Items.OfType<ComboBoxItem>()
            .Any(item => string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase));

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

        var nebKey = NebiusApiKeyBox.Password;
        var tavilyKey = TavilyApiKeyBox.Password;
        var removeNebius = RemoveNebiusKeyCheckBox.IsChecked == true;
        var removeTavily = RemoveTavilyKeyCheckBox.IsChecked == true;
        if ((removeNebius && !string.IsNullOrWhiteSpace(nebKey))
            || (removeTavily && !string.IsNullOrWhiteSpace(tavilyKey)))
        {
            ProviderSettingsStatusText.Text = "For each provider, enter a replacement key or select remove, not both.";
            ProviderTabItemFocus();
            return;
        }

        var candidate = new KeyboardWtfProviderSettings
        {
            NebiusApiKey = string.IsNullOrWhiteSpace(nebKey) ? null : nebKey,
            TavilyApiKey = string.IsNullOrWhiteSpace(tavilyKey) ? null : tavilyKey,
            FastModel = FastModelTextBox.Text.Trim(),
            StandardModel = StandardModelTextBox.Text.Trim(),
            DeepModel = DeepModelTextBox.Text.Trim(),
            AssistantName = AssistantNameTextBox.Text.Trim(),
            AssistantTone = SelectedTag(AssistantToneComboBox),
            SpeakJarvisResponses = SpeakJarvisResponsesCheckBox.IsChecked == true,
            SpeechOutputVoice = SelectedTag(SpeechOutputVoiceComboBox),
            SpeechOutputRate = ParseSelectedTag(SpeechOutputRateComboBox),
            SpeechLanguage = SelectedTag(SpeechLanguageComboBox),
            MaximumListeningSeconds = ParseSelectedTag(MaximumListeningSecondsComboBox),
            EndOfTurnSilenceMilliseconds = ParseSelectedTag(EndOfTurnSilenceComboBox),
            UsePersonalMemory = UsePersonalMemoryCheckBox.IsChecked == true
        };
        if (!candidate.TryValidate(out error))
        {
            var providerFieldsValid = KeyboardWtfProviderSettings.ValidModelId(candidate.FastModel)
                && KeyboardWtfProviderSettings.ValidModelId(candidate.StandardModel)
                && KeyboardWtfProviderSettings.ValidModelId(candidate.DeepModel)
                && IsValidCredential(nebKey)
                && IsValidCredential(tavilyKey);
            if (!providerFieldsValid)
            {
                ProviderSettingsStatusText.Text = error;
                ProviderTabItemFocus();
            }
            else
            {
                VoiceSettingsStatusText.Text = error;
                SettingsTabs.SelectedIndex = 2;
            }
            return;
        }

        var replacingUnreadable = ReplaceUnreadableSettingsCheckBox.IsChecked == true;
        var providerSettingsChanged = !string.IsNullOrWhiteSpace(nebKey)
            || !string.IsNullOrWhiteSpace(tavilyKey)
            || removeNebius
            || removeTavily
            || replacingUnreadable
            || !string.Equals(candidate.FastModel, _providerSettingsSummary.FastModel, StringComparison.Ordinal)
            || !string.Equals(candidate.StandardModel, _providerSettingsSummary.StandardModel, StringComparison.Ordinal)
            || !string.Equals(candidate.DeepModel, _providerSettingsSummary.DeepModel, StringComparison.Ordinal)
            || !string.Equals(candidate.AssistantName, _providerSettingsSummary.AssistantName, StringComparison.Ordinal)
            || !string.Equals(candidate.AssistantTone, _providerSettingsSummary.AssistantTone, StringComparison.Ordinal)
            || candidate.SpeakJarvisResponses != _providerSettingsSummary.SpeakJarvisResponses
            || !string.Equals(candidate.SpeechOutputVoice, _providerSettingsSummary.SpeechOutputVoice, StringComparison.Ordinal)
            || candidate.SpeechOutputRate != _providerSettingsSummary.SpeechOutputRate
            || !string.Equals(candidate.SpeechLanguage, _providerSettingsSummary.SpeechLanguage, StringComparison.Ordinal)
            || candidate.MaximumListeningSeconds != _providerSettingsSummary.MaximumListeningSeconds
            || candidate.EndOfTurnSilenceMilliseconds != _providerSettingsSummary.EndOfTurnSilenceMilliseconds
            || candidate.UsePersonalMemory != _providerSettingsSummary.UsePersonalMemory;

        if (providerSettingsChanged && !_providerSettingsSummary.IsReadable && !replacingUnreadable)
        {
            ProviderSettingsStatusText.Text = "Saved provider settings are unreadable. Choose the explicit replace option or cancel; the existing file will not be overwritten implicitly.";
            ProviderTabItemFocus();
            return;
        }

        if (providerSettingsChanged)
        {
            ProviderSettingsEdit = new KeyboardWtfProviderSettingsEdit(
                string.IsNullOrWhiteSpace(nebKey) ? null : nebKey,
                removeNebius,
                string.IsNullOrWhiteSpace(tavilyKey) ? null : tavilyKey,
                removeTavily,
                candidate.FastModel,
                candidate.StandardModel,
                candidate.DeepModel,
                candidate.AssistantName,
                candidate.AssistantTone,
                candidate.SpeakJarvisResponses,
                candidate.SpeechOutputVoice,
                candidate.SpeechOutputRate,
                candidate.SpeechLanguage,
                candidate.MaximumListeningSeconds,
                candidate.EndOfTurnSilenceMilliseconds,
                candidate.UsePersonalMemory,
                replacingUnreadable);
        }

        ResultSettings = settings;
        ClearSensitiveInputs();
        DialogResult = true;
    }

    private static string SelectedTag(ComboBox comboBox) =>
        (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;

    private static int ParseSelectedTag(ComboBox comboBox) =>
        int.TryParse(SelectedTag(comboBox), out var value) ? value : -1;

    private static bool IsValidCredential(string value) =>
        value.Length <= 4096 && !value.Any(char.IsControl);

    private void ProviderTabItemFocus() => SettingsTabs.SelectedIndex = 0;

    private void ClearSensitiveInputs()
    {
        NebiusApiKeyBox.Clear();
        TavilyApiKeyBox.Clear();
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
