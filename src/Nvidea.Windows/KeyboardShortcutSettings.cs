using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace Nvidea.Windows;

public sealed class KeyboardShortcutSettings
{
    public string Invoke { get; set; } = "Ctrl+Shift+Space";
    public string LocalDictation { get; set; } = "Ctrl+Alt+D";
    public string SmartDictation { get; set; } = "Ctrl+Alt+K";
    public string Jarvis { get; set; } = "Ctrl+Alt+Q";
    public string EmergencyStop { get; set; } = "Ctrl+Alt+X";
    public string OpenSettings { get; set; } = "Ctrl+Alt+,";

    public static KeyboardShortcutSettings Defaults() => new();

    public KeyboardShortcutSettings Copy() => new()
    {
        Invoke = Invoke,
        LocalDictation = LocalDictation,
        SmartDictation = SmartDictation,
        Jarvis = Jarvis,
        EmergencyStop = EmergencyStop,
        OpenSettings = OpenSettings
    };

    public bool TryValidate(out string error)
    {
        var used = new HashSet<(uint Modifiers, uint VirtualKey)>();
        foreach (var (name, value) in Entries())
        {
            if (!ShortcutChord.TryParse(value, out var chord))
            {
                error = $"{name} needs a supported key plus at least one modifier (Ctrl, Alt, Shift, or Win).";
                return false;
            }

            if (!used.Add((chord.Modifiers, chord.VirtualKey)))
            {
                error = $"{name} uses a shortcut already assigned to another action.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    public IEnumerable<(string Name, string Value)> Entries()
    {
        yield return ("Open keyboard-wtf", Invoke);
        yield return ("Local Dictation", LocalDictation);
        yield return ("Smart Dictation", SmartDictation);
        yield return ("Jarvis", Jarvis);
        yield return ("Emergency stop", EmergencyStop);
        yield return ("Open Settings", OpenSettings);
    }
}

internal readonly record struct ShortcutChord(uint Modifiers, uint VirtualKey, string Display)
{
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWindows = 0x0008;

    public static bool TryParse(string? value, out ShortcutChord chord)
    {
        chord = default;
        if (string.IsNullOrWhiteSpace(value) || value.Length > 48)
            return false;

        var parts = value.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Length is < 2 or > 5 || parts.Any(static part => part.Length == 0))
            return false;

        uint modifiers = 0;
        for (var index = 0; index < parts.Length - 1; index++)
        {
            uint modifier = parts[index].ToLowerInvariant() switch
            {
                "ctrl" or "control" => ModControl,
                "alt" => ModAlt,
                "shift" => ModShift,
                "win" or "windows" => ModWindows,
                _ => 0
            };
            if (modifier == 0 || (modifiers & modifier) != 0)
                return false;
            modifiers |= modifier;
        }

        if (modifiers == 0 || !TryParseKey(parts[^1], out var virtualKey, out var keyDisplay))
            return false;

        var modifierDisplay = new List<string>(4);
        if ((modifiers & ModControl) != 0) modifierDisplay.Add("Ctrl");
        if ((modifiers & ModAlt) != 0) modifierDisplay.Add("Alt");
        if ((modifiers & ModShift) != 0) modifierDisplay.Add("Shift");
        if ((modifiers & ModWindows) != 0) modifierDisplay.Add("Win");
        modifierDisplay.Add(keyDisplay);
        chord = new ShortcutChord(modifiers, virtualKey, string.Join('+', modifierDisplay));
        return true;
    }

    public static bool TryCreate(Key key, ModifierKeys modifiers, out ShortcutChord chord)
    {
        chord = default;
        var nativeModifiers = (uint)0;
        if ((modifiers & ModifierKeys.Control) != 0) nativeModifiers |= ModControl;
        if ((modifiers & ModifierKeys.Alt) != 0) nativeModifiers |= ModAlt;
        if ((modifiers & ModifierKeys.Shift) != 0) nativeModifiers |= ModShift;
        if ((modifiers & ModifierKeys.Windows) != 0) nativeModifiers |= ModWindows;
        if (nativeModifiers == 0 || !TryFormatKey(key, out var keyDisplay, out var virtualKey))
            return false;

        var parts = new List<string>(5);
        if ((nativeModifiers & ModControl) != 0) parts.Add("Ctrl");
        if ((nativeModifiers & ModAlt) != 0) parts.Add("Alt");
        if ((nativeModifiers & ModShift) != 0) parts.Add("Shift");
        if ((nativeModifiers & ModWindows) != 0) parts.Add("Win");
        parts.Add(keyDisplay);
        chord = new ShortcutChord(nativeModifiers, virtualKey, string.Join('+', parts));
        return true;
    }

    private static bool TryParseKey(string token, out uint virtualKey, out string display)
    {
        virtualKey = 0;
        display = string.Empty;
        Key key;
        if (token.Equals("Space", StringComparison.OrdinalIgnoreCase)) key = Key.Space;
        else if (token.Equals(",", StringComparison.Ordinal) || token.Equals("Comma", StringComparison.OrdinalIgnoreCase)) key = Key.OemComma;
        else if (token.Equals(".", StringComparison.Ordinal) || token.Equals("Period", StringComparison.OrdinalIgnoreCase)) key = Key.OemPeriod;
        else if (token.Equals("-", StringComparison.Ordinal) || token.Equals("Minus", StringComparison.OrdinalIgnoreCase)) key = Key.OemMinus;
        else if (token.Length == 1 && char.IsAsciiDigit(token[0]) && Enum.TryParse<Key>("D" + token, true, out key)) { }
        else if (!Enum.TryParse(token, true, out key)) return false;

        return TryFormatKey(key, out display, out virtualKey);
    }

    private static bool TryFormatKey(Key key, out string display, out uint virtualKey)
    {
        display = string.Empty;
        virtualKey = 0;
        if (key is Key.None or Key.System or Key.DeadCharProcessed or Key.ImeProcessed or Key.OemClear
            or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return false;

        var name = key.ToString();
        display = key switch
        {
            >= Key.D0 and <= Key.D9 => ((char)('0' + (int)key - (int)Key.D0)).ToString(),
            Key.Space => "Space",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemMinus => "-",
            _ => name
        };
        var native = KeyInterop.VirtualKeyFromKey(key);
        if (native <= 0 || native > ushort.MaxValue)
            return false;
        virtualKey = (uint)native;
        return true;
    }
}

internal sealed record KeyboardShortcutSettingsLoadResult(
    KeyboardShortcutSettings Settings,
    string? Notice,
    bool MigratedFromLegacy = false);

internal static class KeyboardShortcutSettingsStore
{
    private const int MaxSettingsBytes = 4096;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static KeyboardShortcutSettingsLoadResult Load(string path, string? legacySettingsPath = null)
    {
        if (!File.Exists(path))
        {
            if (!string.IsNullOrWhiteSpace(legacySettingsPath) && File.Exists(legacySettingsPath))
                return LoadLegacyHotkeys(legacySettingsPath);

            return new KeyboardShortcutSettingsLoadResult(KeyboardShortcutSettings.Defaults(), null);
        }

        try
        {
            var info = new FileInfo(path);
            if (info.Length is <= 0 or > MaxSettingsBytes)
                throw new InvalidDataException("Settings file size is outside the supported range.");
            var settings = JsonSerializer.Deserialize<KeyboardShortcutSettings>(File.ReadAllBytes(path), JsonOptions)
                ?? throw new InvalidDataException("Settings file is empty.");
            if (!settings.TryValidate(out _))
                throw new InvalidDataException("Settings contain invalid or duplicate shortcuts.");
            return new KeyboardShortcutSettingsLoadResult(settings, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            return new KeyboardShortcutSettingsLoadResult(
                KeyboardShortcutSettings.Defaults(),
                "Saved shortcuts could not be read safely; built-in defaults are active. Open Settings to replace them.");
        }
    }

    private static KeyboardShortcutSettingsLoadResult LoadLegacyHotkeys(string path)
    {
        const int maxLegacySettingsBytes = 1024 * 1024;
        try
        {
            var info = new FileInfo(path);
            if (info.Length is <= 0 or > maxLegacySettingsBytes)
                throw new InvalidDataException("Legacy settings file size is outside the supported range.");

            // The legacy file also contains provider credentials and unrelated preferences.
            // Only the Hotkeys object is projected into the new settings type; no legacy key
            // field is copied into the new app configuration or surfaced to the UI.
            using var document = JsonDocument.Parse(File.ReadAllBytes(path), new JsonDocumentOptions { MaxDepth = 24 });
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !TryGetProperty(document.RootElement, "Hotkeys", out var hotkeys)
                || hotkeys.ValueKind != JsonValueKind.Object)
            {
                return new KeyboardShortcutSettingsLoadResult(
                    KeyboardShortcutSettings.Defaults(),
                    "Existing keyboard.wtf settings were found, but no compatible hotkeys could be imported. The original settings remain unchanged.");
            }

            var settings = KeyboardShortcutSettings.Defaults();
            if (!TryMapLegacyHotkey(hotkeys, "PushToTalk", value => settings.SmartDictation = value)
                || !TryMapLegacyHotkey(hotkeys, "Dictation", value => settings.LocalDictation = value)
                || !TryMapLegacyHotkey(hotkeys, "Jarvis", value => settings.Jarvis = value)
                || !TryMapLegacyHotkey(hotkeys, "Cancel", value => settings.EmergencyStop = value)
                || !TryMapLegacyHotkey(hotkeys, "OpenSettings", value => settings.OpenSettings = value))
            {
                return new KeyboardShortcutSettingsLoadResult(
                    KeyboardShortcutSettings.Defaults(),
                    "Existing keyboard.wtf shortcuts could not be validated, so keyboard-wtf defaults are active. The original settings remain unchanged.");
            }

            if (!settings.TryValidate(out _))
            {
                return new KeyboardShortcutSettingsLoadResult(
                    KeyboardShortcutSettings.Defaults(),
                    "Existing keyboard.wtf shortcuts conflict with keyboard-wtf shortcuts, so defaults are active. The original settings remain unchanged.");
            }

            return new KeyboardShortcutSettingsLoadResult(
                settings,
                "Existing keyboard.wtf shortcuts were imported. The original settings remain unchanged.",
                MigratedFromLegacy: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            return new KeyboardShortcutSettingsLoadResult(
                KeyboardShortcutSettings.Defaults(),
                "Existing keyboard.wtf shortcuts could not be read safely; keyboard-wtf defaults are active. The original settings remain unchanged.");
        }
    }

    private static bool TryMapLegacyHotkey(JsonElement hotkeys, string propertyName, Action<string> assign)
    {
        if (!TryGetProperty(hotkeys, propertyName, out var property))
            return true;
        if (property.ValueKind != JsonValueKind.String)
            return false;
        var value = property.GetString();
        if (!ShortcutChord.TryParse(value, out _))
            return false;
        assign(value!);
        return true;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        var found = false;
        value = default;
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                if (found)
                    throw new JsonException("Legacy settings contain an ambiguous duplicate property.");
                value = property.Value;
                found = true;
            }
        }

        return found;
    }

    public static void Save(string path, KeyboardShortcutSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.TryValidate(out var error))
            throw new InvalidOperationException(error);

        var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Settings path has no parent directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(settings, JsonOptions);
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
