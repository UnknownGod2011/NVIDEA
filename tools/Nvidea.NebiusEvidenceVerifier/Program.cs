using Nvidea.Core.Jobs;

static int Fail(string detail)
{
    Console.Error.WriteLine($"NVIDEA Nebius evidence verification: FAIL {detail}");
    return 1;
}

if (args.Length == 1 && (args[0] is "--help" or "-h"))
{
    Console.WriteLine("Usage: Nvidea.NebiusEvidenceVerifier <redacted-manifest.json> <pass-evidence.json>");
    Console.WriteLine("Verifies that a saved zero-cost preflight manifest and a later live PASS artifact share the exact deployment fingerprint.");
    Console.WriteLine("No credentials, provider calls, secret resolution, or network access are required.");
    return 0;
}

if (args.Length != 2)
    return Fail("Expected exactly two redacted artifact paths. Use --help for usage.");

try
{
    var verification = NebiusResearchDeploymentEvidenceVerifier.VerifyFiles(args[0], args[1]);
    Console.WriteLine("NVIDEA Nebius deployment evidence verification: PASS");
    Console.WriteLine($"Deployment fingerprint (SHA-256): {verification.DeploymentFingerprintSha256}");
    Console.WriteLine($"Completed at (UTC): {verification.CompletedAtUtc:O}");
    Console.WriteLine($"Remote durable stages: {verification.RemoteStageCount}");
    Console.WriteLine($"Evidence items: {verification.EvidenceItemCount}");
    Console.WriteLine($"Validated citations: {verification.ValidatedCitationCount}");
    Console.WriteLine("Credentials/provider calls/secret resolution: not required");
    return 0;
}
catch (Exception exception) when (exception is InvalidDataException or ArgumentException or IOException or UnauthorizedAccessException)
{
    return Fail(exception.Message.ReplaceLineEndings(" "));
}
