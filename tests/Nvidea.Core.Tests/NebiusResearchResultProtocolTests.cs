using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchResultProtocolTests
{
    [Fact]
    public void ResultProtector_RoundTripsCheckpointWithoutPlaintextLeakage()
    {
        using var client = RSA.Create(2048); using var worker = RSA.Create(2048); var now = DateTimeOffset.UtcNow;
        var result = ValidResult(now) with { StepResult = new JobStepResult(false, ResearchJobHandler.EvidenceStep, "{\"question\":\"sensitive roadmap question\",\"evidence\":\"private source material\"}") };
        var envelope = Protect(result, client, worker); var serialized = System.Text.Json.JsonSerializer.Serialize(envelope);
        Assert.DoesNotContain("sensitive roadmap question", serialized, StringComparison.Ordinal); Assert.DoesNotContain("private source material", serialized, StringComparison.Ordinal);
        Assert.Equal(result, Unprotect(envelope, client, worker, now.AddMinutes(1)));
    }

    [Fact]
    public void ResultProtector_RejectsForgedSignature()
    {
        using var client = RSA.Create(2048); using var worker = RSA.Create(2048); var now = DateTimeOffset.UtcNow; var envelope = Protect(ValidResult(now), client, worker);
        var signature = Convert.FromBase64String(envelope.WorkerSignature); signature[0] ^= 1;
        Assert.Throws<CryptographicException>(() => Unprotect(envelope with { WorkerSignature = Convert.ToBase64String(signature) }, client, worker, now.AddMinutes(1)));
    }

    [Fact]
    public void ResultProtector_RejectsWorkerKeyMismatch()
    {
        using var client = RSA.Create(2048); using var worker = RSA.Create(2048); using var impostor = RSA.Create(2048); var now = DateTimeOffset.UtcNow; var envelope = Protect(ValidResult(now), client, worker);
        Assert.Throws<CryptographicException>(() => ResearchResultProtector.Unprotect(envelope, client.ExportPkcs8PrivateKeyPem(), impostor.ExportSubjectPublicKeyInfoPem(), now.AddMinutes(1)));
    }

    [Fact]
    public void ResultProtector_RejectsRemoteJobIdTampering()
    {
        using var client = RSA.Create(2048); using var worker = RSA.Create(2048); var now = DateTimeOffset.UtcNow; var envelope = Protect(ValidResult(now), client, worker);
        Assert.Throws<CryptographicException>(() => Unprotect(envelope with { RemoteJobId = "aijob-substituted" }, client, worker, now.AddMinutes(1)));
    }

    [Fact]
    public void ResultProtector_RejectsOpaqueWorkItemSwap()
    {
        using var client = RSA.Create(2048); using var worker = RSA.Create(2048); var now = DateTimeOffset.UtcNow; var envelope = Protect(ValidResult(now), client, worker); var replacementId = envelope.OpaqueWorkItemId[..^1] + (envelope.OpaqueWorkItemId[^1] == 'A' ? "B" : "A");
        Assert.Throws<CryptographicException>(() => Unprotect(envelope with { OpaqueWorkItemId = replacementId }, client, worker, now.AddMinutes(1)));
    }

    [Fact]
    public void ResultProtector_RejectsCiphertextMutationIncludingCheckpointReceiptPayload()
    {
        using var client = RSA.Create(2048); using var worker = RSA.Create(2048); var now = DateTimeOffset.UtcNow;
        var result = ValidResult(now) with { StepResult = new JobStepResult(true, ResearchJobHandler.CompletedStep, "{\"report\":{\"answerMarkdown\":\"trusted\"},\"receipt\":{\"reportSha256\":\"abc\",\"provenanceSha256\":\"def\"}}") };
        var envelope = Protect(result, client, worker); var ciphertext = Convert.FromBase64String(envelope.Ciphertext); ciphertext[ciphertext.Length / 2] ^= 1;
        Assert.Throws<CryptographicException>(() => Unprotect(envelope with { Ciphertext = Convert.ToBase64String(ciphertext) }, client, worker, now.AddMinutes(1)));
    }

    [Fact]
    public void ResultProtector_RejectsCrossJobReplayEvenWithOriginalSignature()
    {
        using var client = RSA.Create(2048); using var worker = RSA.Create(2048); var now = DateTimeOffset.UtcNow; var first = Protect(ValidResult(now), client, worker); var second = Protect(ValidResult(now) with { RemoteJobId = "aijob-other-456" }, client, worker);
        Assert.Throws<CryptographicException>(() => Unprotect(first with { RemoteJobId = second.RemoteJobId }, client, worker, now.AddMinutes(1)));
    }

    [Fact]
    public void ResultProtector_RejectsCiphertextAndTagSubstitutionFromAnotherValidEnvelope()
    {
        using var client = RSA.Create(2048); using var worker = RSA.Create(2048); var now = DateTimeOffset.UtcNow; var expected = ValidResult(now); var substituted = expected with { StepResult = new JobStepResult(true, ResearchJobHandler.CompletedStep, "{\"report\":\"substituted\",\"receipt\":\"substituted\"}") };
        var first = Protect(expected, client, worker); var second = Protect(substituted, client, worker);
        Assert.Throws<CryptographicException>(() => Unprotect(first with { Ciphertext = second.Ciphertext, AuthenticationTag = second.AuthenticationTag }, client, worker, now.AddMinutes(1)));
    }

    [Fact]
    public void ResultProtector_RejectsExpiredResultBeforeDecrypting()
    {
        using var client = RSA.Create(2048); using var worker = RSA.Create(2048); var now = DateTimeOffset.UtcNow; var result = ValidResult(now); var envelope = Protect(result, client, worker);
        Assert.Throws<InvalidOperationException>(() => Unprotect(envelope, client, worker, result.ExpiresAt.AddSeconds(1)));
    }

    private static ProtectedResearchResultEnvelope Protect(RemoteResearchStageResult result, RSA client, RSA worker) => ResearchResultProtector.Protect(result, client.ExportSubjectPublicKeyInfoPem(), worker.ExportPkcs8PrivateKeyPem());
    private static RemoteResearchStageResult Unprotect(ProtectedResearchResultEnvelope envelope, RSA client, RSA worker, DateTimeOffset now) => ResearchResultProtector.Unprotect(envelope, client.ExportPkcs8PrivateKeyPem(), worker.ExportSubjectPublicKeyInfoPem(), now);
    private static RemoteResearchStageResult ValidResult(DateTimeOffset now) => new(Guid.NewGuid(), ResearchJobHandler.PlannedStep, "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt", "aijob-remote-123", new JobStepResult(false, ResearchJobHandler.EvidenceStep, "{\"prepared\":true}"), now, now.AddHours(2));
}
