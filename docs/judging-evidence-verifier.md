# Unified judging evidence verifier

`Nvidea.JudgingEvidenceVerifier` combines the project’s judge-facing evidence into one bounded, redacted PASS/FAIL summary:

1. deterministic positive Personal AI evaluator output;
2. deterministic adversarial Personal AI evaluator output;
3. matching Nebius deployment-manifest + live PASS evidence; and
4. a **fresh live** Nebius model-catalog PASS for the exact configured Fast / Standard / Deep Nemotron tiers.

The verifier itself is credential-free. It performs no Nebius, Tavily, browser, speech, Ollama, Object Storage, or Serverless calls. Generate the live model-catalog artifact immediately beforehand with `Nvidea.NebiusModelCatalogCheck`.

## Recommended sequence

```powershell
# Zero-inference provider readiness check. Requires NEBIUS_API_KEY but does not issue inference.
dotnet run --project tools/Nvidea.NebiusModelCatalogCheck -- `
  --live `
  --output artifacts/nebius-model-catalog-live.json

# Unify deterministic + live deployment + current catalog evidence.
dotnet run --project tools/Nvidea.JudgingEvidenceVerifier -- `
  artifacts/personal-ai-positive.json `
  artifacts/personal-ai-adversarial.json `
  artifacts/nebius-deployment-manifest.json `
  artifacts/nebius-live-pass.json `
  artifacts/nebius-model-catalog-live.json `
  --output artifacts/judge-evidence-summary.json
```

A successful verifier run exits `0`. Any malformed, incomplete, mismatched, failing, captured, or stale catalog artifact exits `1`.

## Validation boundary

The verifier fails closed unless both synthetic evaluator artifacts use schema version `1`, report `overallPassed=true`, contain the exact required stable check sets with no duplicate IDs, and contain no failed checks. The positive evaluator must also contain its metrics object.

All inputs are size-bounded to 256 KiB, parsed with comments/trailing commas disabled, checked recursively for duplicate JSON property names, and SHA-256 hashed. Unknown JSON properties are rejected for the evaluator and catalog schemas.

Nebius deployment evidence is delegated to the existing `NebiusResearchDeploymentEvidenceVerifier`, which recomputes the redacted deployment-manifest fingerprint, compares it to the live PASS artifact, and validates completion/count invariants.

The model-catalog artifact is a separate provider-readiness boundary. The verifier requires:

- schema `nvidea.nebius-model-catalog-check.v1`;
- `mode: "live"` — captured snapshots are never accepted as proof of current provider readiness;
- `passed: true` with no failure codes;
- an observation no more than **15 minutes old**;
- at most two minutes of tolerated future clock skew;
- a trusted `nebius.com` host;
- a valid catalog SHA-256 and bounded model count; and
- exactly the currently configured Fast / Standard / Deep model IDs, including environment overrides.

This means an old catalog snapshot can still be useful for offline development through `Nvidea.NebiusModelCatalogCheck --input`, but it cannot be promoted into the final current-readiness judge summary.

## Redaction and claims

The emitted judge summary includes only bounded evidence metadata: PASS state, evidence classes, evaluator timestamps/check IDs, artifact hashes, Nebius deployment fingerprint/counts, catalog observation time, catalog host/hash/count, configured required model IDs, and the 900-second catalog freshness window.

It does **not** emit input paths, evaluator detail strings, credentials, Authorization headers, provider error bodies, full provider catalog contents, research prompts/results, browser session data, memory content, cookies, tokens, key material, or secret references.

A green synthetic evaluator proves deterministic contract behavior, not live provider health. A green Nebius deployment evidence pair proves consistency of the supplied deployment/live-run artifacts. A fresh live catalog PASS proves only that the configured model IDs were listed by the provider recently; it does **not** prove quota, structured-output/tool-call support, context limits, chat-completion success, or Serverless execution. Those remain separate validation layers.

## Regression coverage

`tests/Nvidea.JudgingEvidenceVerifier.Tests` covers the catalog-readiness boundary, including fresh-live PASS, captured evidence rejection, stale evidence rejection, excessive future clock skew, lookalike-host rejection, mismatched configured model IDs, and contradictory PASS + failure-code evidence.

## Determinism

The success summary does not add a verifier wall-clock timestamp. The current time is used only to enforce the catalog freshness gate. For identical still-fresh input bytes and metadata, emitted JSON content is stable apart from platform newline handling when persisted. Artifact hashes make the exact reviewed evidence bytes tamper-evident once the summary is saved alongside them.
