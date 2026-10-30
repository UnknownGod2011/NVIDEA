using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchResultProtocolTests
{
    [Fact]
    public void ResultProtector_RoundTripsCheckpointWithoutPlaintextLeakage()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var result = ValidResult(now) with
        {
            StepResult = new JobStepResult(
                Completed: false,
                RequiresApproval: false,
                CheckpointStep: ResearchJobHandler.EvidenceStep,
                CheckpointPayload: "{\"question\":\"sensitive roadmap question\",\"evidence\":\"private source material\"}")
        };

        var envelope = ResearchResultProtector.Protect(result, rsa.ExportSubjectPublicKeyInfoPem());
        var serialized = System.Text.Json.JsonSerializer.Serialize(envelope);

        Assert.DoesNotContain("sensitive roadmap question", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("private source material", serialized, StringComparison.Ordinal);
        Assert.Equal(result, ResearchResultProtector.Unprotect(
            envelope,
            rsa.ExportPkcs8PrivateKeyPem(),
            now.AddMinutes(1)));
    }

    [Fact]
    public void ResultProtector_RejectsRemoteJobIdTampering()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var envelope = ResearchResultProtector.Protect(ValidResult(now), rsa.ExportSubjectPublicKeyInfoPem());

        Assert.ThrowsAny<CryptographicException>(() => ResearchResultProtector.Unprotect(
            envelope with { RemoteJobId = "aijob-substituted" },
            rsa.ExportPkcs8PrivateKeyPem(),
            now.AddMinutes(1)));
    }

    [Fact]
    public void ResultProtector_RejectsOpaqueWorkItemSwap()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var envelope = ResearchResultProtector.Protect(ValidResult(now), rsa.ExportSubjectPublicKeyInfoPem());
        var id = envelope.OpaqueWorkItemId[..^1] + (envelope.OpaqueWorkItemId[^1] == 'A' ? "B" : "A");

        Assert.ThrowsAny<CryptographicException>(() => ResearchResultProtector.Unprotect(
            envelope with { OpaqueWorkItemId = id },
            rsa.ExportPkcs8PrivateKeyPem(),
            now.AddMinutes(1)));
    }

    [Fact]
    public void ResultProtector_RejectsCiphertextMutationIncludingCheckpointReceiptPayload()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var result = ValidResult(now) with
        {
            StepResult = new JobStepResult(
                Completed: true,
                RequiresApproval: false,
                CheckpointStep: ResearchJobHandler.CompletedStep,
                CheckpointPayload: "{\"report\":{\"answerMarkdown\":\"trusted\"},\"receipt\":{\"reportSha256\":\"abc\",\"provenanceSha256\":\"def\"}}")
        };
        var envelope = ResearchResultProtector.Protect(result, rsa.ExportSubjectPublicKeyInfoPem());
        var ciphertext = Convert.FromBase64String(envelope.Ciphertext);
        ciphertext[ciphertext.Length / 2] ^= 1;

        Assert.ThrowsAny<CryptographicException>(() => ResearchResultProtector.Unprotect(
            envelope with { Ciphertext = Convert.ToBase64String(ciphertext) },
            rsa.ExportPkcs8PrivateKeyPem(),
            now.AddMinutes(1)));
    }

    [Fact]
    public void ResultProtector_RejectsCiphertextAndTagSubstitutionFromAnotherValidEnvelope()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var expected = ValidResult(now);
        var substituted = expected with
        {
            StepResult = new JobStepResult(
                Completed: true,
                RequiresApproval: false,
                CheckpointStep: ResearchJobHandler.CompletedStep,
                CheckpointPayload: "{\"report\":\"substituted\",\"receipt\":\"substituted\"}")
        };
        var first = ResearchResultProtector.Protect(expected, rsa.ExportSubjectPublicKeyInfoPem());
        var second = ResearchResultProtector.Protect(substituted, rsa.ExportSubjectPublicKeyInfoPem());

        Assert.ThrowsAny<CryptographicException>(() => ResearchResultProtector.Unprotect(
            first with { Ciphertext = second.Ciphertext, AuthenticationTag = second.AuthenticationTag },
            rsa.ExportPkcs8PrivateKeyPem(),
            now.AddMinutes(1)));
    }

    [Fact]
    public void ResultProtector_RejectsExpiredResultBeforeDecrypting()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var result = ValidResult(now);
        var envelope = ResearchResultProtector.Protect(result, rsa.ExportSubjectPublicKeyInfoPem());

        Assert.Throws<InvalidOperationException>(() => ResearchResultProtector.Unprotect(
            envelope,
            rsa.ExportPkcs8PrivateKeyPem(),
            result.ExpiresAt.AddSeconds(1)));
    }

    private static RemoteResearchStageResult ValidResult(DateTimeOffset now) => new(
        Guid.NewGuid(),
        ResearchJobHandler.PlannedStep,
        "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt",
        "aijob-remote-123",
        new JobStepResult(
            Completed: false,
            RequiresApproval: false,
            CheckpointStep: ResearchJobHandler.EvidenceStep,
            CheckpointPayload: "{\"prepared\":true}"),
        now,
        now.AddHours(2));
}
