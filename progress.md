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
- `NebiusResearchDeploymentEvidenceVerifier` + `tools/Nvidea.NebiusEvidenceVerifier` provide a zero-network, zero-secret reproducibility check between saved preflight and later live PASS artifacts.
- Evidence verification now rejects unknown JSON members, duplicate property names at any depth, comments, trailing commas, and excessive nesting before accepting a judging artifact.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + reproducible live evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, `--live-research`, zero-cost `--live-research-preflight`, reproducible/redacted deployment fingerprints, optional MysteryBox version IDs, machine-readable PASS evidence, reusable atomic artifact persistence, strict MysteryBox resource-id validation, a directly testable live configuration loader, and an independent deployment-evidence verifier CLI.

### 2026-09-10 — Current run: strict judging-evidence ingestion
Completed:
- Re-read `progress.md` completely before mutation and inspected the current repo tree, live configuration loader, deployment-evidence verifier, regression tests, README, and verifier CLI.
- Hardened `src/Nvidea.Core/Jobs/NebiusResearchDeploymentEvidenceVerifier.cs`.
- Added strict .NET 8 unmapped-member rejection so unknown top-level or nested JSON fields fail closed instead of being silently ignored.
- Added a bounded JSON parse pre-pass (`MaxDepth = 32`) that rejects comments and trailing commas.
- Added recursive duplicate-property detection using ordinal property-name comparison for every JSON object, including nested deployment structures and arrays of objects. This prevents ambiguous last-property-wins interpretation from weakening evidence verification.
- Retained the existing 256 KiB file/input bound, schema validation, PASS invariant validation, manifest fingerprint recomputation, and fixed-time fingerprint comparisons.
- Added `tests/Nvidea.Core.Tests/NebiusResearchDeploymentEvidenceStrictJsonTests.cs` covering unknown top-level members, unknown nested members, duplicate top-level/nested properties, comments, trailing commas, and excessive nesting.
- Added `docs/judging-evidence.md` with the intended zero-cost preflight → explicit live run → zero-network verifier workflow, redaction boundary, PASS emission conditions, verifier guarantees, and the explicit limitation that the pair is internally reproducible evidence rather than third-party attestation.
- Verified against current Microsoft .NET documentation that `JsonSerializerOptions.UnmappedMemberHandling = Disallow` is available starting in .NET 8 and raises `JsonException` for unmapped payload members.

Commits this run:
- `4d0ee23e19f0fb302cab69160159509651f2db6c` — harden deployment evidence JSON verification.
- `a6cac1268565f6039a745fe180412e6092142840` — add strict JSON evidence verifier regression coverage.
- `7ee7b56938c576cf2ae3e0d83cf4a332169af723` — add judging evidence verification workflow.

Validation / evidence:
- Repository identity was explicitly re-verified immediately before every GitHub mutation; every write targeted exactly `UnknownGod2011/NVIDEA`.
- Static review confirms strict parsing occurs before typed deserialization and before any fingerprint comparison.
- Static review confirms duplicate JSON property names are rejected recursively rather than relying on serializer last-write behavior.
- Current Microsoft .NET documentation confirms unmapped-member rejection through `JsonUnmappedMemberHandling.Disallow` is supported in .NET 8.
- No live Nebius credentials/resources were used, and no provider/network call is required by the verifier.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- A usable .NET SDK/runtime is still not available in this execution environment, so compilation/test execution is **not claimed**.

Security / privacy / failure review:
- The verifier consumes only already-redacted artifacts and never needs Serverless tokens, Object Storage credentials, MysteryBox IDs, PEM material, provider responses, research bodies, bucket names, or resource IDs.
- Artifact reads remain bounded before content is loaded.
- Unknown fields now fail closed, preventing a future or malicious producer from smuggling unverified semantics into evidence that an older verifier would otherwise ignore.
- Duplicate property names fail closed at every object depth, avoiding parser differential/ambiguity attacks.
- Comments, trailing commas, malformed JSON, unsupported schemas, excessive nesting, self-inconsistent manifests, and cross-artifact mismatches fail closed.
- The verifier still explicitly does not claim external authorship/attestation; replacing both redacted artifacts can produce another internally consistent pair.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; the new code is statically reviewed but not compiled/executed.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- Exact provider acceptance of the configured Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the hidden worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the real contract succeeds.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.

## Single Best Next Task
Do the next compile-oriented hardening pass on the live boundary: add focused tests for default/min/max poll and total-timeout values, research-question limits, invalid RSA private-key material, unreadable output destinations, malformed/oversized evidence files, and file-path redaction. Then surface `docs/judging-evidence.md` from the main README and, if a .NET-capable environment becomes available, run the focused verifier/configuration tests plus both zero-cost CLIs before attempting the first credential-backed Nebius Serverless contract.
