using System.Text.RegularExpressions;

namespace Nvidea.Core.Memory;

public sealed partial class DefaultMemoryWritePolicy : IMemoryWritePolicy
{
    private const int MaxKeyLength = 160;
    private const int MaxContentLength = 16_000;

    public MemoryWriteDecision Evaluate(MemoryWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Key))
            return MemoryWriteDecision.Deny("Memory key cannot be empty.");
        if (string.IsNullOrWhiteSpace(request.Content))
            return MemoryWriteDecision.Deny("Memory content cannot be empty.");
        if (request.Key.Length > MaxKeyLength)
            return MemoryWriteDecision.Deny($"Memory key exceeds {MaxKeyLength} characters.");
        if (request.Content.Length > MaxContentLength)
            return MemoryWriteDecision.Deny($"Memory content exceeds {MaxContentLength} characters.");
        if (request.Importance is < 0 or > 1)
            return MemoryWriteDecision.Deny("Memory importance must be between 0 and 1.");
        if (request.Confidence is < 0 or > 1)
            return MemoryWriteDecision.Deny("Memory confidence must be between 0 and 1.");

        var combined = $"{request.Key}\n{request.Content}";
        if (LooksLikeSecret(combined))
            return MemoryWriteDecision.Deny("Potential credentials, private keys, or authentication secrets must never be persisted in personal memory.");

        if (request.Sensitivity is MemorySensitivity.Sensitive or MemorySensitivity.Restricted)
        {
            if (!request.ExplicitUserApproval)
                return MemoryWriteDecision.Deny("Sensitive memory requires explicit user approval before persistence.");
        }

        if (request.Retention == MemoryRetention.Indefinite && !request.ExplicitUserApproval)
            return MemoryWriteDecision.Deny("Indefinite retention requires explicit user approval.");

        return MemoryWriteDecision.Allow();
    }

    private static bool LooksLikeSecret(string value)
    {
        if (PrivateKeyHeaderRegex().IsMatch(value))
            return true;
        if (BearerTokenRegex().IsMatch(value))
            return true;
        if (CommonSecretAssignmentRegex().IsMatch(value))
            return true;
        if (JwtRegex().IsMatch(value))
            return true;

        return false;
    }

    [GeneratedRegex("-----BEGIN (?:RSA |EC |OPENSSH |PGP )?PRIVATE KEY-----", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PrivateKeyHeaderRegex();

    [GeneratedRegex(@"\bBearer\s+[A-Za-z0-9._~+/-]{16,}={0,2}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex(@"\b(?:api[_-]?key|secret|password|passwd|access[_-]?token|refresh[_-]?token)\s*[:=]\s*[\"']?[^\s\"']{8,}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CommonSecretAssignmentRegex();

    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex JwtRegex();
}
