# Judging Evidence Workflow

NVIDEA's live Nebius research path is designed so a successful demo can be tied to the exact deployment configuration that was preflighted, without publishing credentials or secret identifiers.

## 1. Run zero-cost preflight

Configure the documented `NVIDEA_LIVE_*` environment variables, including optional output locations:

```powershell
$env:NVIDEA_LIVE_REDACTED_MANIFEST_PATH = ".\artifacts\nebius-preflight.json"
$env:NVIDEA_LIVE_PASS_EVIDENCE_PATH = ".\artifacts\nebius-pass.json"

dotnet run --project .\tools\Nvidea.NebiusContractProbe\Nvidea.NebiusContractProbe.csproj -- --live-research-preflight
```

The preflight performs local configuration/cryptographic/topology validation only. It does not submit a Serverless job, call Nemotron, call Tavily, resolve MysteryBox secrets, or access Object Storage.

The optional redacted manifest contains deployment-shape commitments rather than raw infrastructure secrets. It includes the immutable worker-image digest, compute/storage topology, public-key fingerprints, redacted secret-reference commitments, and the deterministic deployment SHA-256 fingerprint.

## 2. Run the explicit live research contract

Only after preflight succeeds:

```powershell
dotnet run --project .\tools\Nvidea.NebiusContractProbe\Nvidea.NebiusContractProbe.csproj -- --live-research
```

A PASS evidence artifact is written only after the durable remote research flow completes and the final report has non-empty evidence plus validated citations. Failure, timeout, cancellation, or invalid final evidence must not produce a PASS artifact.

The PASS artifact contains only:

- deployment fingerprint;
- UTC completion timestamp;
- completed remote-stage count;
- evidence-item count;
- validated-citation count.

It intentionally excludes access tokens, Object Storage keys, MysteryBox IDs/version IDs, bucket names, Serverless job IDs, PEM contents, research payloads, URLs, provider responses, and citation text.

## 3. Verify the preflight and PASS pair offline

```powershell
dotnet run --project .\tools\Nvidea.NebiusEvidenceVerifier\Nvidea.NebiusEvidenceVerifier.csproj -- .\artifacts\nebius-preflight.json .\artifacts\nebius-pass.json
```

The verifier requires no credentials and makes no provider/network calls. It:

1. bounds each artifact to 256 KiB before reading;
2. rejects comments, trailing commas, excessive JSON nesting, duplicate property names, and unknown JSON members;
3. validates both schema versions and PASS metric invariants;
4. recomputes the deployment fingerprint from the redacted manifest contents;
5. rejects a manifest changed after fingerprinting;
6. compares the recomputed manifest fingerprint with the PASS fingerprint using fixed-time comparison.

A successful result demonstrates that the supplied PASS artifact corresponds to the exact redacted deployment configuration represented by the supplied preflight manifest.

## Security boundary

This workflow proves internal reproducibility consistency, not third-party cryptographic attestation. Someone who maliciously replaces both artifacts can construct another internally consistent pair. The evidence verifier therefore must not be described as external proof that Nebius authored the run.

For judges, the intended evidence chain is stronger when the live terminal output, generated redacted artifacts, architecture view, and visible Nebius/Nemotron/Tavily execution are captured together in the demo recording.
