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
/// Append-only audit trail with per-record local protection, a deterministic hash chain and,
/// when a local-state protector is available, an independently protected tail seal. The seal
/// uses a write-ahead pending state so crashes between sealing and append can be recovered without
/// silently accepting final-record truncation.
/// </summary>
public sealed class JsonLinesAuditTrail : IAuditTrail
{
    private const int CurrentFormatVersion = 1;
    private const int TailSealVersion = 1;
    private const string ProtectionPurpose = "audit-event-v1";
    private const string TailSealPurpose = "audit-tail-seal-v1";
    private const string CommittedSealState = "committed";
    private const string PendingSealState = "pending";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _path;
    private readonly string _sealPath;
    private readonly ILocalStateProtector? _protector;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonLinesAuditTrail(string path, ILocalStateProtector? protector = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Audit path is required.", nameof(path));

        _path = Path.GetFullPath(path);
        _sealPath = _path + ".seal";
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

            var line = CreateLine(auditEvent, state.Events.Count + 1L, state.LastHash);
            if (_protector is not null)
            {
                await WriteSealAsync(
                    new AuditTailSeal(
                        TailSealVersion,
                        PendingSealState,
                        state.Events.Count,
                        state.LastHash,
                        line.Sequence,
                        line.Hash),
                    cancellationToken).ConfigureAwait(false);
            }

            await File.AppendAllTextAsync(
                _path,
                JsonSerializer.Serialize(line, JsonOptions) + Environment.NewLine,
                Encoding.UTF8,
                cancellationToken).ConfigureAwait(false);

            if (_protector is not null)
            {
                await WriteSealAsync(
                    new AuditTailSeal(
                        TailSealVersion,
                        CommittedSealState,
                        line.Sequence,
                        line.Hash,
                        null,
                        null),
                    cancellationToken).ConfigureAwait(false);
            }
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
            return (await LoadAndMigrateAsync(cancellationToken).ConfigureAwait(false)).Events;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<AuditState> LoadAndMigrateAsync(CancellationToken cancellationToken)
    {
        AuditState state;
        if (!File.Exists(_path))
        {
            state = new AuditState(Array.Empty<AuditEvent>(), string.Empty);
        }
        else
        {
            var lines = await File.ReadAllLinesAsync(_path, cancellationToken).ConfigureAwait(false);
            var meaningful = lines.Where(static x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (meaningful.Length == 0)
            {
                state = new AuditState(Array.Empty<AuditEvent>(), string.Empty);
            }
            else if (LooksLikeCurrentFormat(meaningful[0]))
            {
                state = ParseCurrentFormat(meaningful);
            }
            else
            {
                var legacy = ParseLegacyEvents(meaningful);
                var migratedLines = BuildLines(legacy);
                await RewriteAsCurrentFormatAsync(migratedLines, cancellationToken).ConfigureAwait(false);
                var lastHash = migratedLines.Count == 0 ? string.Empty : migratedLines[^1].Hash;
                state = new AuditState(legacy, lastHash);
            }
        }

        if (_protector is not null)
            await VerifyOrRecoverTailSealAsync(state, cancellationToken).ConfigureAwait(false);

        return state;
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
            if (!FixedTimeTextEquals(line.PreviousHash ?? string.Empty, previousHash))
                throw new InvalidDataException("Audit trail hash-chain predecessor check failed.");

            var expectedHash = ComputeHash(line.Version, line.Sequence, previousHash, line.Protected, line.Payload);
            if (!FixedTimeTextEquals(expectedHash, line.Hash))
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

    private async Task VerifyOrRecoverTailSealAsync(AuditState state, CancellationToken cancellationToken)
    {
        if (!File.Exists(_sealPath))
        {
            await WriteSealAsync(
                new AuditTailSeal(
                    TailSealVersion,
                    CommittedSealState,
                    state.Events.Count,
                    state.LastHash,
                    null,
                    null),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var seal = await ReadSealAsync(cancellationToken).ConfigureAwait(false);
        ValidateSealShape(seal);

        if (seal.State == CommittedSealState)
        {
            if (state.Events.Count != seal.CommittedSequence || !FixedTimeTextEquals(state.LastHash, seal.CommittedHash))
                throw new InvalidDataException("Audit tail seal does not match the audit chain; final-record truncation or replacement may have occurred.");
            return;
        }

        var matchesCommitted = state.Events.Count == seal.CommittedSequence
            && FixedTimeTextEquals(state.LastHash, seal.CommittedHash);
        var matchesPending = seal.PendingSequence.HasValue
            && state.Events.Count == seal.PendingSequence.Value
            && FixedTimeTextEquals(state.LastHash, seal.PendingHash!);

        if (matchesPending)
        {
            await WriteSealAsync(
                new AuditTailSeal(
                    TailSealVersion,
                    CommittedSealState,
                    seal.PendingSequence!.Value,
                    seal.PendingHash!,
                    null,
                    null),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (matchesCommitted)
        {
            await WriteSealAsync(
                new AuditTailSeal(
                    TailSealVersion,
                    CommittedSealState,
                    seal.CommittedSequence,
                    seal.CommittedHash,
                    null,
                    null),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        throw new InvalidDataException("Audit tail seal is pending but matches neither the pre-append nor post-append chain state.");
    }

    private async Task<AuditTailSeal> ReadSealAsync(CancellationToken cancellationToken)
    {
        var persisted = await File.ReadAllBytesAsync(_sealPath, cancellationToken).ConfigureAwait(false);
        LocalStatePayload decoded;
        try
        {
            decoded = LocalStateEnvelope.Decode(persisted, _protector!, TailSealPurpose);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(persisted);
        }

        if (!decoded.WasProtected)
        {
            CryptographicOperations.ZeroMemory(decoded.Plaintext);
            throw new InvalidDataException("Audit tail seal exists without the required protected local-state envelope.");
        }

        try
        {
            return JsonSerializer.Deserialize<AuditTailSeal>(decoded.Plaintext, JsonOptions)
                ?? throw new InvalidDataException("Audit tail seal was empty after deserialization.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Audit tail seal contains malformed JSON.", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(decoded.Plaintext);
        }
    }

    private async Task WriteSealAsync(AuditTailSeal seal, CancellationToken cancellationToken)
    {
        ValidateSealShape(seal);
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(seal, JsonOptions);
        byte[] encoded;
        try
        {
            encoded = LocalStateEnvelope.Encode(plaintext, _protector!, TailSealPurpose);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }

        var tempPath = _sealPath + ".write-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(tempPath, encoded, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, _sealPath, overwrite: true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encoded);
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static void ValidateSealShape(AuditTailSeal seal)
    {
        if (seal.Version != TailSealVersion)
            throw new InvalidDataException("Unsupported audit tail-seal version.");
        if (seal.CommittedSequence < 0)
            throw new InvalidDataException("Audit tail seal contains a negative committed sequence.");
        if (seal.CommittedSequence == 0 && !string.IsNullOrEmpty(seal.CommittedHash))
            throw new InvalidDataException("Empty audit tail seal must use an empty committed hash.");
        if (seal.CommittedSequence > 0 && !IsSha256Hex(seal.CommittedHash))
            throw new InvalidDataException("Audit tail seal contains an invalid committed hash.");

        if (seal.State == CommittedSealState)
        {
            if (seal.PendingSequence is not null || seal.PendingHash is not null)
                throw new InvalidDataException("Committed audit tail seal cannot contain pending state.");
            return;
        }

        if (seal.State != PendingSealState)
            throw new InvalidDataException("Audit tail seal contains an unsupported state.");
        if (seal.PendingSequence != seal.CommittedSequence + 1)
            throw new InvalidDataException("Pending audit tail seal must describe exactly one append after the committed tail.");
        if (!IsSha256Hex(seal.PendingHash))
            throw new InvalidDataException("Pending audit tail seal contains an invalid pending hash.");
    }

    private async Task RewriteAsCurrentFormatAsync(IReadOnlyList<AuditLine> lines, CancellationToken cancellationToken)
    {
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

        try
        {
            var payload = Convert.ToBase64String(payloadBytes);
            var hash = ComputeHash(CurrentFormatVersion, sequence, previousHash, isProtected, payload);
            return new AuditLine(CurrentFormatVersion, sequence, previousHash, isProtected, payload, hash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(payloadBytes);
        }
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
            {
                CryptographicOperations.ZeroMemory(persisted);
                throw new InvalidDataException("Audit trail is protected but no local-state protector is available in this runtime.");
            }

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

    private static bool FixedTimeTextEquals(string expected, string actual)
    {
        if (expected.Length != actual.Length)
            return false;
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(actual));
    }

    private static bool IsSha256Hex(string? value)
    {
        if (value is null || value.Length != 64)
            return false;
        foreach (var character in value)
        {
            if (!Uri.IsHexDigit(character))
                return false;
        }
        return true;
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

    private sealed record AuditTailSeal(
        int Version,
        string State,
        long CommittedSequence,
        string CommittedHash,
        long? PendingSequence,
        string? PendingHash);
}
