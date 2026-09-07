using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nvidea.Core.Security;

namespace Nvidea.Core.Capabilities;

public sealed record AuditEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string CapabilityId,
    string ActionId,
    string EventType,
    CapabilityRiskLevel Risk,
    bool Allowed,
    bool Approved,
    string ApprovalScope,
    string Summary,
    IReadOnlyDictionary<string, string>? Metadata = null);

public interface IAuditTrail
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Append-only audit trail with per-record local protection and a deterministic hash chain.
/// On Windows, CurrentUser DPAPI is used by default. Existing plaintext JSONL files are
/// migrated only after every legacy event has parsed successfully.
/// </summary>
public sealed class JsonLinesAuditTrail : IAuditTrail
{
    private const int CurrentFormatVersion = 1;
    private const string ProtectionPurpose = "audit-event-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _path;
    private readonly ILocalStateProtector? _protector;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonLinesAuditTrail(string path, ILocalStateProtector? protector = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Audit path is required.", nameof(path));

        _path = Path.GetFullPath(path);
        _protector = protector ?? (OperatingSystem.IsWindows() ? new WindowsDpapiLocalStateProtector() : null);
    }

    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ValidateEvent(auditEvent);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var state = await LoadAndMigrateAsync(cancellationToken).ConfigureAwait(false);
            if (state.Events.Any(x => x.EventId == auditEvent.EventId))
                throw new InvalidOperationException($"Audit event '{auditEvent.EventId}' already exists; audit records are append-only.");

            var previousHash = state.LastHash;
            var line = CreateLine(auditEvent, state.Events.Count + 1L, previousHash);
            var serialized = JsonSerializer.Serialize(line, JsonOptions);
            await File.AppendAllTextAsync(_path, serialized + Environment.NewLine, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadAndMigrateAsync(cancellationToken).ConfigureAwait(false);
            return state.Events;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<AuditState> LoadAndMigrateAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
            return new AuditState(Array.Empty<AuditEvent>(), string.Empty);

        var lines = await File.ReadAllLinesAsync(_path, cancellationToken).ConfigureAwait(false);
        var meaningful = lines.Where(static x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (meaningful.Length == 0)
            return new AuditState(Array.Empty<AuditEvent>(), string.Empty);

        if (LooksLikeCurrentFormat(meaningful[0]))
            return ParseCurrentFormat(meaningful);

        var legacy = ParseLegacyEvents(meaningful);
        await RewriteAsCurrentFormatAsync(legacy, cancellationToken).ConfigureAwait(false);
        var lastHash = legacy.Count == 0 ? string.Empty : BuildLines(legacy)[^1].Hash;
        return new AuditState(legacy, lastHash);
    }

    private AuditState ParseCurrentFormat(IReadOnlyList<string> lines)
    {
        var events = new List<AuditEvent>(lines.Count);
        var previousHash = string.Empty;
        long expectedSequence = 1;

        foreach (var raw in lines)
        {
            AuditLine line;
            try
            {
                line = JsonSerializer.Deserialize<AuditLine>(raw, JsonOptions)
                    ?? throw new InvalidDataException("Audit line was empty after deserialization.");
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("Audit trail contains malformed protected-format JSON.", ex);
            }

            if (line.Version != CurrentFormatVersion || line.Sequence != expectedSequence)
                throw new InvalidDataException("Audit trail version/sequence integrity check failed.");
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(line.PreviousHash ?? string.Empty),
                    Encoding.ASCII.GetBytes(previousHash)))
                throw new InvalidDataException("Audit trail hash-chain predecessor check failed.");

            var expectedHash = ComputeHash(line.Version, line.Sequence, previousHash, line.Protected, line.Payload);
            if (!FixedTimeHexEquals(expectedHash, line.Hash))
                throw new InvalidDataException("Audit trail hash-chain integrity check failed.");

            var auditEvent = DecodeEvent(line);
            ValidateEvent(auditEvent);
            events.Add(auditEvent);
            previousHash = line.Hash;
            expectedSequence++;
        }

        return new AuditState(events, previousHash);
    }

    private List<AuditEvent> ParseLegacyEvents(IReadOnlyList<string> lines)
    {
        var result = new List<AuditEvent>(lines.Count);
        foreach (var line in lines)
        {
            AuditEvent parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<AuditEvent>(line, JsonOptions)
                    ?? throw new InvalidDataException("Legacy audit line was empty after deserialization.");
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("Legacy audit trail contains malformed JSON; migration was aborted.", ex);
            }

            ValidateEvent(parsed);
            if (result.Any(x => x.EventId == parsed.EventId))
                throw new InvalidDataException($"Legacy audit trail contains duplicate event id '{parsed.EventId}'; migration was aborted.");
            result.Add(parsed);
        }

        return result;
    }

    private async Task RewriteAsCurrentFormatAsync(IReadOnlyList<AuditEvent> events, CancellationToken cancellationToken)
    {
        var lines = BuildLines(events);
        var tempPath = _path + ".migrate-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var serialized = string.Join(Environment.NewLine, lines.Select(x => JsonSerializer.Serialize(x, JsonOptions)));
            if (serialized.Length > 0)
                serialized += Environment.NewLine;
            await File.WriteAllTextAsync(tempPath, serialized, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private List<AuditLine> BuildLines(IReadOnlyList<AuditEvent> events)
    {
        var result = new List<AuditLine>(events.Count);
        var previousHash = string.Empty;
        for (var index = 0; index < events.Count; index++)
        {
            var line = CreateLine(events[index], index + 1L, previousHash);
            result.Add(line);
            previousHash = line.Hash;
        }
        return result;
    }

    private AuditLine CreateLine(AuditEvent auditEvent, long sequence, string previousHash)
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(auditEvent, JsonOptions);
        byte[] payloadBytes;
        var isProtected = _protector is not null;
        try
        {
            payloadBytes = isProtected
                ? _protector!.Protect(plaintext, ProtectionPurpose)
                : plaintext.ToArray();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }

        var payload = Convert.ToBase64String(payloadBytes);
        if (isProtected)
            CryptographicOperations.ZeroMemory(payloadBytes);
        var hash = ComputeHash(CurrentFormatVersion, sequence, previousHash, isProtected, payload);
        return new AuditLine(CurrentFormatVersion, sequence, previousHash, isProtected, payload, hash);
    }

    private AuditEvent DecodeEvent(AuditLine line)
    {
        byte[] persisted;
        try
        {
            persisted = Convert.FromBase64String(line.Payload);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException("Audit payload is not valid base64.", ex);
        }

        byte[] plaintext;
        if (line.Protected)
        {
            if (_protector is null)
                throw new InvalidDataException("Audit trail is protected but no local-state protector is available in this runtime.");
            try
            {
                plaintext = _protector.Unprotect(persisted, ProtectionPurpose);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidDataException("Audit event could not be decrypted for this user/device context.", ex);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(persisted);
            }
        }
        else
        {
            plaintext = persisted;
        }

        try
        {
            return JsonSerializer.Deserialize<AuditEvent>(plaintext, JsonOptions)
                ?? throw new InvalidDataException("Audit payload was empty after deserialization.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Audit payload contains malformed event JSON.", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static bool LooksLikeCurrentFormat(string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("version", out _)
                && doc.RootElement.TryGetProperty("sequence", out _)
                && doc.RootElement.TryGetProperty("payload", out _)
                && doc.RootElement.TryGetProperty("hash", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string ComputeHash(int version, long sequence, string previousHash, bool isProtected, string payload)
    {
        var canonical = $"{version}\n{sequence}\n{previousHash}\n{(isProtected ? 1 : 0)}\n{payload}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static bool FixedTimeHexEquals(string expected, string actual)
    {
        if (expected.Length != actual.Length)
            return false;
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(actual));
    }

    private static void ValidateEvent(AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        if (auditEvent.EventId == Guid.Empty)
            throw new ArgumentException("Audit events require a non-empty event id.", nameof(auditEvent));
        if (string.IsNullOrWhiteSpace(auditEvent.CapabilityId) || string.IsNullOrWhiteSpace(auditEvent.ActionId))
            throw new ArgumentException("Audit events require capability and action identifiers.", nameof(auditEvent));
    }

    private sealed record AuditLine(
        int Version,
        long Sequence,
        string PreviousHash,
        bool Protected,
        string Payload,
        string Hash);

    private sealed record AuditState(IReadOnlyList<AuditEvent> Events, string LastHash);
}
