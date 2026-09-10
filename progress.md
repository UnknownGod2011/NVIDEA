# NVIDEA Hackathon Progress

## Mission
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon. Target **Personal AI**, **Best Use of Tavily**, and top-three / Grand Prize quality. NVIDEA must remain independently stronger than keyboard.wtf in NVIDIA/Nebius-first reasoning, memory, research, browser automation, long-running work, verification, privacy, and safety.

## Hard Repository Boundary
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate it.
- Never write to any other repository.
- Before every GitHub mutation, verify the target is exactly `UnknownGod2011/NVIDEA`.
- Do not remove working functionality merely to simplify implementation.

## Current Product / Architecture State
- .NET 8 core in `src/Nvidea.Core`; WPF host in `src/Nvidea.Windows`; deployable remote worker in `src/Nvidea.Worker`.
- NVIDIA Nemotron through Nebius Token Factory with structured reasoning/tool boundaries, retries, timeout/cancellation, and conservative routing.
- Layered personal memory with privacy-aware writes and hybrid retrieval.
- Tavily Search + Extract research with canonical deduplication, evidence quality/freshness/diversity, provenance, untrusted-evidence handling, validated citations, and restart-safe staged checkpoints.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Remote research uses encrypted opaque work items, signed authoritative Nebius resource-ID bindings, two-phase dispatch, crash reconciliation, provider lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, and a non-root worker image.
- Native Windows-side S3-compatible Object Storage transport publishes encrypted work items/bindings/results directly through Nebius Object Storage while the worker consumes the same bucket prefix through a Serverless-mounted directory.
- Deployment preflight enforces exact Object Storage ↔ Serverless mount alignment, READ_WRITE transport, MysteryBox-backed worker credentials, digest-pinned worker image, RSA strength/identity consistency, bounded compute/storage settings, and a redacted reproducible deployment fingerprint.
- `NebiusResearchLiveConfigurationLoader` owns the complete live environment/file parsing and preflight construction path for both zero-cost and paid live modes.
- `Nvidea.NebiusContractProbe` supports cheap planner, `--live-research-preflight`, and explicit real `--live-research` modes. Both live modes consume the same validated configuration object and fingerprint.
- PASS evidence and redacted deployment manifests use `AtomicTextArtifactWriter` for crash-safe same-directory replacement.
- `NebiusResearchDeploymentEvidenceVerifier` + `tools/Nvidea.NebiusEvidenceVerifier` now provide a zero-network, zero-secret reproducibility check between saved preflight and later live PASS artifacts.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + reproducible live evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, `--live-research`, zero-cost `--live-research-preflight`, reproducible/redacted deployment fingerprints, optional MysteryBox version IDs, machine-readable PASS evidence, reusable atomic artifact persistence, strict MysteryBox resource-id validation, and a directly testable live configuration loader.

### 2026-09-10 — Current run: independent deployment-evidence verification
Completed:
- Re-read `progress.md` completely before mutation and inspected recent commits, the extracted live configuration loader, deployment manifest model, PASS evidence model, contract probe, and existing regression-test style.
- Added `src/Nvidea.Core/Jobs/NebiusResearchDeploymentEvidenceVerifier.cs`.
- The verifier reads only redacted manifest/PASS artifacts, imposes a 256 KiB per-artifact limit, validates both schema versions, validates deployment fingerprints, reuses the production PASS builder for UTC/count invariants, and returns only safe summary evidence.
- Static review identified and fixed a subtle integrity issue: comparing only the two stored fingerprint fields would accept a manifest whose contents had been edited without updating the fingerprint. The verifier now deterministically recomputes SHA-256 from the redacted manifest's unsigned canonical deployment fields and rejects a self-inconsistent manifest before comparing it to PASS evidence.
- Manifest self-integrity and manifest↔PASS equality use fixed-time byte comparison after strict 64-hex validation.
- Added `tests/Nvidea.Core.Tests/NebiusResearchDeploymentEvidenceVerifierTests.cs` covering matching artifacts, manifest/PASS fingerprint mismatch, post-fingerprint manifest tampering, unsupported schemas, invalid PASS counts, and bounded file-based verification.
- Added standalone `tools/Nvidea.NebiusEvidenceVerifier` CLI so operators/judges can run the reproducibility check without loading live credentials or invoking Nebius/Tavily/Object Storage.
- Added `docs/nebius-evidence-verification.md` documenting generation/verification flow, privacy boundaries, and the important limitation that this proves internal reproducibility consistency rather than third-party authorship/attestation.

Commits this run:
- `6d9e5d60eb48377cd1895a7493d79d9ffb8691eb` — add redacted deployment evidence verifier.
- `f7437eddafe7a4b924212d93d12e83c61671939a` — add verifier regression tests.
- `c7ac2a151e2d8a14f4f45a6ec34b25c727f21fee` — add verifier CLI project.
- `97c986d4c588c0262a79de2733b2e1a14600c691` — expose zero-network verifier CLI.
- `34e73ad0675fc11e755f1312fd165ec764a2df72` — recompute manifest fingerprint during verification.
- `1cd16ffe4553551d067220d2b30373ff84aac427` — cover manifest self-integrity in verifier tests.
- `3e66d9359fb7e62dc22ff7769b2c77f5a401a4f7` — document deployment evidence verification.

Validation / evidence:
- Repository identity was explicitly re-verified immediately before every GitHub mutation; every write targeted exactly `UnknownGod2011/NVIDEA`.
- `dotnet --info` was checked in the execution runtime and `dotnet` is not installed, so compilation/test execution is **not claimed**.
- Static review confirms the new verifier requires no provider clients, credentials, secret resolution, or network calls.
- Static review confirms malformed, oversized, unsupported-schema, self-inconsistent, and cross-artifact-mismatched evidence fails closed.
- No live Nebius credentials/resources were available, so Object Storage/Serverless/Nemotron/Tavily execution is **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- The verifier consumes only already-redacted artifacts and never needs Serverless tokens, Object Storage credentials, MysteryBox IDs, PEM material, provider responses, research bodies, bucket names, or resource IDs.
- File-read failures report only the artifact class, not file contents or secret-bearing configuration.
- Artifact size is bounded before reading; malformed JSON and unsupported schemas fail closed.
- Fingerprints are validated as exact 64-character hexadecimal SHA-256 values before fixed-time comparison.
- The verifier explicitly does not claim third-party authenticity: replacing both redacted artifacts can create another internally consistent pair. This limitation is documented rather than overstated.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; new code is statically reviewed but not compiled/executed.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- Exact provider acceptance of the configured Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the hidden worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the real contract succeeds.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The verifier proves deployment-evidence consistency, not external authorship/attestation; stronger signed provenance could be added later if judging needs it.

## Single Best Next Task
Do a compile-oriented hardening pass on the new verifier and live configuration boundary: add default-value and edge-boundary tests for poll/timeout/question parsing, invalid RSA private-key material, unreadable output destinations, malformed/oversized evidence files, and duplicate/unknown JSON fields as appropriate; then make the verifier tooling visible from the main README/judging workflow. If a .NET-capable environment becomes available, run the focused unit tests and both zero-cost CLIs before attempting the first credential-backed Nebius Serverless research contract.
