# Unified judging evidence verifier

`Nvidea.JudgingEvidenceVerifier` combines the project's three evidence classes into one bounded, redacted PASS/FAIL summary:

1. deterministic positive Personal AI evaluator output;
2. deterministic adversarial Personal AI evaluator output; and
3. matching Nebius deployment-manifest + live PASS evidence.

It is intentionally credential-free. The verifier performs no Nebius, Tavily, browser, speech, Ollama, Object Storage, or Serverless calls.

## Usage

```powershell
dotnet run --project tools/Nvidea.JudgingEvidenceVerifier -- `
  artifacts/personal-ai-positive.json `
  artifacts/personal-ai-adversarial.json `
  artifacts/nebius-deployment-manifest.json `
  artifacts/nebius-live-pass.json `
  --output artifacts/judge-evidence-summary.json
```

A successful run exits `0`. Any malformed, incomplete, mismatched, or failing artifact exits `1`.

## Validation boundary

The verifier fails closed unless both synthetic evaluator artifacts use schema version `1`, report `overallPassed=true`, contain the exact required stable check sets with no duplicate IDs, and contain no failed checks. The positive evaluator must also contain its metrics object.

All four inputs are size-bounded to 256 KiB, parsed with comments/trailing commas disabled, checked recursively for duplicate JSON property names, and hashed with SHA-256. Unknown JSON properties are rejected for the evaluator schemas.

Nebius evidence is delegated to the existing `NebiusResearchDeploymentEvidenceVerifier`, which recomputes the redacted deployment-manifest fingerprint, compares it to the live PASS artifact in constant time, and validates the recorded completion/count invariants. This keeps the live-cloud trust boundary canonical rather than reimplementing it in the judge tool.

## Redaction and claims

The emitted judge summary includes only:

- PASS state;
- evidence class (`synthetic` or `live`);
- evaluator timestamps and stable check IDs;
- SHA-256 hashes of the supplied artifacts;
- the redacted Nebius deployment fingerprint;
- bounded Nebius evidence counts; and
- explicit claims that remain outside the evidence boundary.

It does **not** emit input file paths, evaluator detail strings, credentials, provider errors, research prompts/results, browser session data, memory content, cookies, tokens, key material, or secret references.

A green synthetic evaluator proves deterministic contract behavior, not live provider health. A green Nebius evidence pair proves consistency of the supplied redacted deployment/live-run artifacts; it is not third-party attestation and does not independently prove WPF, Playwright, Tavily, speech, Ollama, or every Serverless execution path.

## Determinism

The success summary does not add a verifier wall-clock timestamp. For identical input bytes, evaluator metadata, and Nebius evidence, the emitted JSON content is stable apart from platform newline handling when persisted. Artifact hashes make the exact evidence bytes reviewable and tamper-evident once the summary is saved alongside them.
