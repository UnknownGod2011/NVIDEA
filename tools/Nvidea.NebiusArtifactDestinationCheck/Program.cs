using Nvidea.Core.Jobs;

try
{
    if (args.Any(arg => string.Equals(arg, "--help", StringComparison.Ordinal) || string.Equals(arg, "-h", StringComparison.Ordinal)))
    {
        Console.WriteLine("Usage: Nvidea.NebiusArtifactDestinationCheck");
        Console.WriteLine("Reads NVIDEA_LIVE_REDACTED_MANIFEST_PATH and NVIDEA_LIVE_PASS_EVIDENCE_PATH.");
        Console.WriteLine("Creates only randomized sibling probe files, deletes them immediately, and never creates or modifies the final evidence files.");
        return 0;
    }

    if (args.Length != 0)
    {
        Console.Error.WriteLine("NVIDEA artifact destination check: FAIL [arguments] This command accepts no arguments. Use --help.");
        return 1;
    }

    var report = NebiusResearchArtifactDestinationPreflight.ValidateFromEnvironment();
    Console.WriteLine("NVIDEA artifact destination check: PASS");
    Console.WriteLine($"Configured destinations: {report.WritableDestinationCount}");
    Console.WriteLine($"Redacted manifest destination: {(report.RedactedManifestConfigured ? "writable" : "not configured")}");
    Console.WriteLine($"PASS evidence destination: {(report.PassEvidenceConfigured ? "writable" : "not configured")}");
    Console.WriteLine("Final evidence artifacts created or modified: no");
    Console.WriteLine("Provider/network calls: none");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"NVIDEA artifact destination check: FAIL [destination] {ex.Message.ReplaceLineEndings(" ")}");
    return 1;
}
