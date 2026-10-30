using System.Security.Cryptography;
using System.Text.Json;
using Nvidea.Core.Security;

namespace Nvidea.Core.Nebius;

/// <summary>
/// User-scoped Token Factory, Tavily, voice, and assistant configuration. Credentials are persisted
/// only inside the Windows DPAPI-protected local-state envelope; model and behavior choices are settings.
/// </summary>
public sealed record KeyboardWtfProviderSettings
{
    public string? NebiusApiKey { get; init; }
    public string? TavilyApiKey { get; init; }
    public string FastModel { get; init; } = NebiusOptions.VerifiedNemotronNanoModel;
    public string StandardModel { get; init; } = NebiusOptions.VerifiedNemotronSuperModel;
    public string DeepModel { get; init; } = NebiusOptions.VerifiedNemotronUltraModel;
    public string AssistantName { get; init; } = "Jarvis";
    public string AssistantTone { get; init; } = "Balanced";
    public bool SpeakJarvisResponses { get; init; } = true;
    public string SpeechOutputVoice { get; init; } = "System default";
    public int SpeechOutputRate { get; init; }
    public string SpeechLanguage { get; init; } = "auto";
    public int MaximumListeningSeconds { get; init; } = 35;
    public int EndOfTurnSilenceMilliseconds { get; init; } = 900;
    public bool UsePersonalMemory { get; init; } = true;

    public static KeyboardWtfProviderSettings Defaults() => new();

    public bool TryValidate(out string error)
    {
        if (!ValidCredential(NebiusApiKey) || !ValidCredential(TavilyApiKey))
        {
            error = "Provider keys must be at most 4,096 characters and cannot contain control characters.";
            return false;
        }

        if (!ValidModelId(FastModel) || !ValidModelId(StandardModel) || !ValidModelId(DeepModel))
        {
            error = "Each model must be a valid Token Factory model identifier (letters, digits, dot, underscore, slash, or hyphen; at most 160 characters).";
            return false;
        }

        if (string.IsNullOrWhiteSpace(AssistantName)
            || AssistantName.Length > 32
            || !char.IsLetter(AssistantName[0])
            || !AssistantName.Skip(1).All(static c => char.IsLetterOrDigit(c) || c is '_' or '-'))
        {
            error = "Assistant name must be a 1–32 character label using letters, digits, underscore, or hyphen.";
            return false;
        }

        if (AssistantTone is not ("Balanced" or "Concise" or "Friendly" or "Professional"))
        {
            error = "Choose a supported assistant tone.";
            return false;
        }

        if (SpeechOutputVoice is null
            || SpeechOutputVoice.Length > 128
            || SpeechOutputVoice.Any(char.IsControl)
            || SpeechOutputRate is < -5 or > 5)
        {
            error = "Choose a valid local speech voice and a rate from -5 to 5.";
            return false;
        }

        if (SpeechLanguage is null
            || (SpeechLanguage != "auto"
                && (SpeechLanguage.Length is < 2 or > 16
                    || !SpeechLanguage.All(static c => char.IsAsciiLetter(c) || c == '-'))))
        {
            error = "Choose Automatic or an installed Windows speech language.";
            return false;
        }

        if (MaximumListeningSeconds is < 5 or > 120
            || EndOfTurnSilenceMilliseconds is < 300 or > 3_000)
        {
            error = "Listening duration must be 5–120 seconds and end-of-turn silence 300–3,000 milliseconds.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool ValidModelId(string? model) =>
        !string.IsNullOrWhiteSpace(model)
        && model.Length <= 160
        && char.IsAsciiLetterOrDigit(model[0])
        && model.All(static c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '/' or '-');

    private static bool ValidCredential(string? key) =>
        key is null || (key.Length <= 4096 && !key.Any(char.IsControl));
}

/// <summary>A redacted projection for the settings UI. It never contains credential values.</summary>
public sealed record KeyboardWtfProviderSettingsSummary(
    bool HasSavedNebiusApiKey,
    bool HasSavedTavilyApiKey,
    string FastModel,
    string StandardModel,
    string DeepModel,
    string AssistantName = "Jarvis",
    string AssistantTone = "Balanced",
    bool SpeakJarvisResponses = true,
    string SpeechOutputVoice = "System default",
    int SpeechOutputRate = 0,
    string SpeechLanguage = "auto",
    int MaximumListeningSeconds = 35,
    int EndOfTurnSilenceMilliseconds = 900,
    bool UsePersonalMemory = true,
    bool IsReadable = true,
    string? Notice = null);

public sealed record KeyboardWtfProviderSettingsEdit(
    string? NebiusApiKeyReplacement,
    bool RemoveNebiusApiKey,
    string? TavilyApiKeyReplacement,
    bool RemoveTavilyApiKey,
    string FastModel,
    string StandardModel,
    string DeepModel,
    string AssistantName = "Jarvis",
    string AssistantTone = "Balanced",
    bool SpeakJarvisResponses = true,
    string SpeechOutputVoice = "System default",
    int SpeechOutputRate = 0,
    string SpeechLanguage = "auto",
    int MaximumListeningSeconds = 35,
    int EndOfTurnSilenceMilliseconds = 900,
    bool UsePersonalMemory = true,
    bool ReplaceUnreadableSettings = false);

/// <summary>
/// Stores local provider settings and assistant preferences under a purpose-bound DPAPI envelope.
/// Plain JSON or an unreadable protected file is never treated as a valid configured credential store.
/// </summary>
public static class KeyboardWtfProviderSettingsStore
{
    private const string ProtectionPurpose = "keyboard-wtf.provider-settings.v1";
    private const int MaxPersistedBytes = 64 * 1024;
    private const int MaxPlaintextBytes = 32 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static string DefaultPath
    {
        get
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData))
                throw new InvalidOperationException("Local application data directory is unavailable.");
            return Path.Combine(localAppData, "keyboard-wtf", "provider-settings.dat");
        }
    }

    public static KeyboardWtfProviderSettings Load(string path, ILocalStateProtector protector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(protector);
        if (!File.Exists(path))
            return KeyboardWtfProviderSettings.Defaults();

        var persisted = File.ReadAllBytes(path);
        byte[]? plaintext = null;
        try
        {
            if (persisted.Length is <= 0 or > MaxPersistedBytes || !LocalStateEnvelope.HasProtectedHeader(persisted))
                throw new InvalidDataException("Saved provider settings are not in the required protected format.");

            var decoded = LocalStateEnvelope.Decode(persisted, protector, ProtectionPurpose);
            plaintext = decoded.Plaintext;
            if (!decoded.WasProtected || plaintext.Length is <= 0 or > MaxPlaintextBytes)
                throw new InvalidDataException("Saved provider settings are outside the supported protected format.");

            var settings = JsonSerializer.Deserialize<KeyboardWtfProviderSettings>(plaintext, JsonOptions)
                ?? throw new InvalidDataException("Saved provider settings are empty.");
            if (!settings.TryValidate(out _))
                throw new InvalidDataException("Saved provider settings failed validation.");
            return settings;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(persisted);
            if (plaintext is not null)
                CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public static KeyboardWtfProviderSettingsSummary LoadSummary(string path, ILocalStateProtector protector)
    {
        try
        {
            var settings = Load(path, protector);
            return new KeyboardWtfProviderSettingsSummary(
                !string.IsNullOrWhiteSpace(settings.NebiusApiKey),
                !string.IsNullOrWhiteSpace(settings.TavilyApiKey),
                settings.FastModel,
                settings.StandardModel,
                settings.DeepModel,
                settings.AssistantName,
                settings.AssistantTone,
                settings.SpeakJarvisResponses,
                settings.SpeechOutputVoice,
                settings.SpeechOutputRate,
                settings.SpeechLanguage,
                settings.MaximumListeningSeconds,
                settings.EndOfTurnSilenceMilliseconds,
                settings.UsePersonalMemory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException or InvalidDataException)
        {
            return new KeyboardWtfProviderSettingsSummary(
                false,
                false,
                NebiusOptions.VerifiedNemotronNanoModel,
                NebiusOptions.VerifiedNemotronSuperModel,
                NebiusOptions.VerifiedNemotronUltraModel,
                IsReadable: false,
                Notice: "Saved provider settings could not be unlocked or validated. Re-enter the keys to replace this settings file, or use environment variables.");
        }
    }

    public static void Save(string path, KeyboardWtfProviderSettings settings, ILocalStateProtector protector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(protector);
        if (!settings.TryValidate(out var error))
            throw new InvalidOperationException(error);

        var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Provider settings path has no parent directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(settings, JsonOptions);
        byte[]? protectedBytes = null;
        try
        {
            if (plaintext.Length > MaxPlaintextBytes)
                throw new InvalidDataException("Provider settings exceed the supported size.");
            protectedBytes = LocalStateEnvelope.Encode(plaintext, protector, ProtectionPurpose);
            if (protectedBytes.Length > MaxPersistedBytes)
                throw new InvalidDataException("Protected provider settings exceed the supported size.");

            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                stream.Write(protectedBytes);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            if (protectedBytes is not null)
                CryptographicOperations.ZeroMemory(protectedBytes);
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    public static void SaveChanges(
        string path,
        KeyboardWtfProviderSettingsEdit edit,
        ILocalStateProtector protector)
    {
        ArgumentNullException.ThrowIfNull(edit);
        ArgumentNullException.ThrowIfNull(protector);

        KeyboardWtfProviderSettings current;
        try
        {
            current = Load(path, protector);
        }
        catch (Exception ex) when (edit.ReplaceUnreadableSettings
            && (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException or InvalidDataException))
        {
            current = KeyboardWtfProviderSettings.Defaults();
        }

        if (edit.RemoveNebiusApiKey && !string.IsNullOrWhiteSpace(edit.NebiusApiKeyReplacement))
            throw new InvalidOperationException("Enter a Nebius key or choose remove, not both.");
        if (edit.RemoveTavilyApiKey && !string.IsNullOrWhiteSpace(edit.TavilyApiKeyReplacement))
            throw new InvalidOperationException("Enter a Tavily key or choose remove, not both.");

        var updated = current with
        {
            NebiusApiKey = edit.RemoveNebiusApiKey
                ? null
                : string.IsNullOrWhiteSpace(edit.NebiusApiKeyReplacement) ? current.NebiusApiKey : edit.NebiusApiKeyReplacement,
            TavilyApiKey = edit.RemoveTavilyApiKey
                ? null
                : string.IsNullOrWhiteSpace(edit.TavilyApiKeyReplacement) ? current.TavilyApiKey : edit.TavilyApiKeyReplacement,
            FastModel = edit.FastModel?.Trim() ?? string.Empty,
            StandardModel = edit.StandardModel?.Trim() ?? string.Empty,
            DeepModel = edit.DeepModel?.Trim() ?? string.Empty,
            AssistantName = edit.AssistantName?.Trim() ?? string.Empty,
            AssistantTone = edit.AssistantTone,
            SpeakJarvisResponses = edit.SpeakJarvisResponses,
            SpeechOutputVoice = edit.SpeechOutputVoice,
            SpeechOutputRate = edit.SpeechOutputRate,
            SpeechLanguage = edit.SpeechLanguage,
            MaximumListeningSeconds = edit.MaximumListeningSeconds,
            EndOfTurnSilenceMilliseconds = edit.EndOfTurnSilenceMilliseconds,
            UsePersonalMemory = edit.UsePersonalMemory
        };
        Save(path, updated, protector);
    }
}
