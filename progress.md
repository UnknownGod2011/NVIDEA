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
- Evidence verification rejects unknown JSON members, duplicate property names at any depth, comments, trailing commas, excessive nesting, oversized artifacts, malformed artifacts, self-inconsistent manifests, and cross-artifact fingerprint mismatches.
- Live RSA role validation now proves that the configured client signing PEM can actually perform a private-key signature and rejects private-key PEM material in the worker-public-key slot.
- Main README now accurately distinguishes the implemented explicit Serverless contract path from still-unverified production WPF remote execution and links the reproducible judging-evidence workflow.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + reproducible live evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, `--live-research`, zero-cost `--live-research-preflight`, reproducible/redacted deployment fingerprints, optional MysteryBox version IDs, machine-readable PASS evidence, reusable atomic artifact persistence, strict MysteryBox resource-id validation, a directly testable live configuration loader, and an independent deployment-evidence verifier CLI.

### 2026-09-10 — Strict judging-evidence ingestion
Hardened deployment-evidence verification with .NET 8 unmapped-member rejection, bounded strict JSON parsing, recursive duplicate-property detection, schema checks, manifest-fingerprint recomputation, fixed-time fingerprint comparison, and `docs/judging-evidence.md` describing the zero-cost preflight → live run → offline verification workflow.

### 2026-09-10 — Current run: RSA role + boundary hardening
Completed:
- Re-read `progress.md` completely before any mutation and inspected the current repo tree, `NebiusResearchLiveConfigurationLoader`, preflight, evidence verifier, existing regression tests, and main README.
- Found and fixed a real live-preflight defect: `RSA.ImportFromPem` accepts public-only RSA PEMs, so a public key supplied in `NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE` could previously survive import/public-key derivation and fail only later when dispatch attempted to sign.
- Hardened `NebiusResearchLiveDryRunPreflight.ValidateSigningIdentity` to perform a harmless fixed-hash RSA signature before a live run is accepted. Public-only or otherwise unusable private-key material now fails closed before any provider/client construction.
- Hardened worker envelope key-role validation so a PEM containing private-key material is rejected when the deployment expects a public-only worker envelope key. This reduces accidental propagation of private material into deployment/evidence plumbing that never needs it.
- Expanded `NebiusResearchLiveConfigurationLoaderTests` with documented defaults, inclusive min/max poll and total-timeout boundaries, invalid total-timeout boundaries, exact research-question maximum and overflow, malformed private-key path redaction, public-only signing-key rejection, and private-key-in-public-slot rejection.
- Expanded `NebiusResearchDeploymentEvidenceVerifierTests` with malformed file ingestion and >256 KiB artifact rejection, including checks that error messages do not leak filesystem paths.
- Updated the main README to remove stale claims that Serverless dispatch is merely a future target. It now explicitly states that the Serverless contract path is implemented but not yet credential-backed/live-validated, while production Windows research remains local until that proof exists.
- Added a README judging-evidence section linking `docs/judging-evidence.md` and documenting the deterministic preflight → live PASS → offline verifier flow and its non-attestation limitation.

Commits this run:
- `603429ee3846a48bf0c6332489b1fc958f356c1b` — harden live RSA key-role validation.
- `5353c268c9a877bfc90df8ede9ecc5762dd6a8c2` — expand live configuration boundary tests.
- `38388b3a9ef896da47ee653dbd4881f27475b2b7` — cover malformed and oversized evidence files.
- `36c36e847d03b9b044d9f62869b3bc7fc191f547` — persist intermediate boundary-hardening progress.
- `a34c3e6ede5fea32a03ccebce10f5497f6831206` — clarify Serverless readiness and judging evidence in README.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation; all writes targeted exactly `UnknownGod2011/NVIDEA`.
- Static review confirms the new signing-capability proof runs inside zero-cost live preflight, after RSA import/strength checks and before the provider-backed live runtime is constructed.
- Static review confirms worker public-key validation rejects PEM labels containing private-key material before RSA import/use.
- Static review confirms the new configuration tests exercise default, minimum, maximum, overflow, malformed-key, role-confusion, and path-redaction cases without real credentials.
- Static review confirms evidence file tests exercise malformed and oversized file paths before any successful evidence verification.
- Main README was reviewed against the current repo architecture and no longer claims that Serverless is entirely unwired.
- `dotnet` remains unavailable in this execution environment, so compilation and test execution are **not claimed**.
- No live Nebius credentials/resources were used and no GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- Private signing material is never logged or included in judging evidence; the preflight only proves usability via an in-memory signature over a fixed empty-input SHA-256 hash.
- A public-only key can no longer masquerade as the dispatch signing key until paid execution begins.
- Worker envelope configuration now fails closed if a private-key PEM is supplied where only a public key should exist, reducing accidental secret exposure.
- PEM and evidence ingestion errors remain path-redacted; tests explicitly check that temporary root/file names do not appear in error text.
- Evidence reads remain bounded at 256 KiB before JSON parsing.
- README wording deliberately distinguishes implemented code from live-validated production capability to avoid overstating hackathon evidence.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; the new code is statically reviewed but not compiled/executed.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- Exact provider acceptance of the configured Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the hidden worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the real contract succeeds.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.

## Single Best Next Task
Harden output/evidence persistence before the first paid run: add direct `AtomicTextArtifactWriter` tests for directory targets, existing-file replacement, write failures and temp-file cleanup; then add a zero-cost operator/self-check command that validates all configured artifact destinations are writable without creating the final manifest/PASS files. If a .NET-capable environment becomes available, immediately run the focused live-configuration/evidence/atomic-writer tests and both zero-cost CLIs before attempting the first credential-backed Serverless contract.
