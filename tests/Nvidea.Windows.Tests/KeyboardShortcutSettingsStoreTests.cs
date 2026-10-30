using System.IO;
using System.Text.Json;
using Nvidea.Windows;

namespace Nvidea.Windows.Tests;

public sealed class KeyboardShortcutSettingsStoreTests
{
    [Fact]
    public void First_run_imports_only_compatible_hotkeys_and_preserves_original_file()
    {
        using var paths = new TemporarySettingsPaths();
        const string legacyJson = """
            {
              "LegacyProviderKey": "legacy-provider-key-marker",
              "Hotkeys": {
                "PushToTalk": "Ctrl+Shift+K",
                "Dictation": "Ctrl+Shift+D",
                "Jarvis": "Ctrl+Shift+J",
                "Cancel": "Ctrl+Shift+X",
                "OpenSettings": "Ctrl+Shift+O"
              }
            }
            """;
        File.WriteAllText(paths.Legacy, legacyJson);

        var loaded = KeyboardShortcutSettingsStore.Load(paths.Current, paths.Legacy);

        Assert.True(loaded.MigratedFromLegacy);
        Assert.Equal("Ctrl+Shift+Space", loaded.Settings.Invoke);
        Assert.Equal("Ctrl+Shift+K", loaded.Settings.SmartDictation);
        Assert.Equal("Ctrl+Shift+D", loaded.Settings.LocalDictation);
        Assert.Equal("Ctrl+Shift+J", loaded.Settings.Jarvis);
        Assert.Equal("Ctrl+Shift+X", loaded.Settings.EmergencyStop);
        Assert.Equal("Ctrl+Shift+O", loaded.Settings.OpenSettings);

        KeyboardShortcutSettingsStore.Save(paths.Current, loaded.Settings);
        Assert.Equal(legacyJson, File.ReadAllText(paths.Legacy));
        var currentJson = File.ReadAllText(paths.Current);
        Assert.DoesNotContain("legacy-provider-key-marker", currentJson, StringComparison.Ordinal);
        Assert.DoesNotContain("LegacyProviderKey", currentJson, StringComparison.OrdinalIgnoreCase);
        var saved = JsonSerializer.Deserialize<KeyboardShortcutSettings>(currentJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(saved);
        Assert.Equal(loaded.Settings.Invoke, saved.Invoke);
        Assert.Equal(loaded.Settings.LocalDictation, saved.LocalDictation);
        Assert.Equal(loaded.Settings.SmartDictation, saved.SmartDictation);
        Assert.Equal(loaded.Settings.Jarvis, saved.Jarvis);
        Assert.Equal(loaded.Settings.EmergencyStop, saved.EmergencyStop);
        Assert.Equal(loaded.Settings.OpenSettings, saved.OpenSettings);
    }

    [Fact]
    public void Existing_keyboard_wtf_settings_take_precedence_over_legacy_file()
    {
        using var paths = new TemporarySettingsPaths();
        var existing = KeyboardShortcutSettings.Defaults();
        existing.Invoke = "Ctrl+Shift+F1";
        KeyboardShortcutSettingsStore.Save(paths.Current, existing);
        File.WriteAllText(paths.Legacy, "{\"Hotkeys\":{\"PushToTalk\":\"Ctrl+Shift+K\"}}");

        var loaded = KeyboardShortcutSettingsStore.Load(paths.Current, paths.Legacy);

        Assert.False(loaded.MigratedFromLegacy);
        Assert.Null(loaded.Notice);
        Assert.Equal("Ctrl+Shift+F1", loaded.Settings.Invoke);
        Assert.Equal(KeyboardShortcutSettings.Defaults().SmartDictation, loaded.Settings.SmartDictation);
    }

    [Fact]
    public void Conflicting_legacy_hotkeys_fail_closed_to_defaults()
    {
        using var paths = new TemporarySettingsPaths();
        File.WriteAllText(paths.Legacy,
            "{\"Hotkeys\":{\"PushToTalk\":\"Ctrl+Shift+K\",\"Dictation\":\"Ctrl+Shift+K\"}}");

        var loaded = KeyboardShortcutSettingsStore.Load(paths.Current, paths.Legacy);

        Assert.False(loaded.MigratedFromLegacy);
        Assert.Contains("conflict", loaded.Notice, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(KeyboardShortcutSettings.Defaults().SmartDictation, loaded.Settings.SmartDictation);
        Assert.Equal("{\"Hotkeys\":{\"PushToTalk\":\"Ctrl+Shift+K\",\"Dictation\":\"Ctrl+Shift+K\"}}",
            File.ReadAllText(paths.Legacy));
    }

    [Fact]
    public void Ambiguous_duplicate_legacy_properties_fail_closed_to_defaults()
    {
        using var paths = new TemporarySettingsPaths();
        const string legacyJson = "{\"Hotkeys\":{\"PushToTalk\":\"Ctrl+Shift+K\",\"pushtotalk\":\"Ctrl+Shift+J\"}}";
        File.WriteAllText(paths.Legacy, legacyJson);

        var loaded = KeyboardShortcutSettingsStore.Load(paths.Current, paths.Legacy);

        Assert.False(loaded.MigratedFromLegacy);
        Assert.NotNull(loaded.Notice);
        Assert.Equal(KeyboardShortcutSettings.Defaults().SmartDictation, loaded.Settings.SmartDictation);
        Assert.Equal(legacyJson, File.ReadAllText(paths.Legacy));
    }

    private sealed class TemporarySettingsPaths : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "keyboard-wtf-settings-tests", Guid.NewGuid().ToString("N"));

        public string Current => Path.Combine(_directory, "keyboard-settings.json");
        public string Legacy => Path.Combine(_directory, "keyboard.wtf", "settings.json");

        public TemporarySettingsPaths()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Legacy)!);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
    }
}
