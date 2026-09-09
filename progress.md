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
- `NebiusResearchDeploymentPreflight` enforces exact S3 bucket/prefix ↔ Serverless mount alignment, `READ_WRITE` transport, MysteryBox-backed worker credentials, and required verification-key topology.
- `NebiusResearchLiveDryRunPreflight` adds zero-cost validation for digest-pinned worker image, compute/storage shape, RSA strength, and exact client signing/public-key correspondence.
- `Nvidea.NebiusContractProbe` provides default planner, `--live-research-preflight`, and opt-in real `--live-research` modes. No production Serverless claim is made until a credential-backed end-to-end PASS exists.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + live preflight
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, `--live-research`, and zero-cost `--live-research-preflight`.

### 2026-09-10 — Current run: reproducible/redacted deployment evidence
Completed:
- Re-read `progress.md` completely and inspected the live dry-run preflight, Serverless secret-reference model, current deployment topology, and existing tests before changing anything.
- Refreshed official MysteryBox documentation. Nebius exposes explicit secret versions and `version_id`; omitting a version selects the primary version, so version-specific references are the correct reproducibility mechanism while secret-level references remain useful for development. Official documentation checked: https://docs.nebius.com/mysterybox/secrets/get .
- Added `NebiusResearchDeploymentManifestBuilder` and typed manifest records in `src/Nvidea.Core/Jobs/NebiusResearchDeploymentManifest.cs`.
- The manifest commits to the immutable worker image digest, project-independent compute shape, Object Storage/mounted-prefix topology, worker/client RSA public-key fingerprints, and worker secret-reference classifications.
- Raw access tokens, S3 static credentials, project/subnet identifiers, PEM bodies, MysteryBox secret ids/version ids, bucket name, provider bodies, and research payloads are excluded from the manifest.
- Bucket identity is represented only as SHA-256. MysteryBox references are represented by `primary-version` versus `version-pinned` plus a SHA-256 commitment to the provider reference. This lets two run manifests detect secret rotation without publishing the MysteryBox identifier.
- Added a deterministic `DeploymentFingerprintSha256` over the redacted manifest body.
- Added `NebiusResearchLivePreflightReporter`, which runs the existing zero-cost live validation and returns only the redacted manifest plus counts of version-pinned versus primary-version worker secrets. It exposes `AllWorkerSecretsVersionPinned` without resolving or returning any secret value/reference.
- Added regression coverage in `NebiusResearchDeploymentManifestTests.cs` for deterministic fingerprints, secret/bucket/project/token redaction, primary-vs-version-pinned classification, fingerprint changes when a pinned version changes, and zero-cost pin-coverage reporting.

Commits this run:
- `b299fc20daae7b64deaf43c0605d2d81c2c06984` — add redacted live deployment manifest/fingerprint.
- `c3425d06558951f6c9f9d34e7cbfc3579c5db188` — add reproducible live preflight report.
- `76a98df3eb945d68df9fc9121bf644c73e46bc14` — bind fingerprint to redacted MysteryBox reference commitments.
- `0e93a2b95d8675b60fdb12d55aeed811ac2af799` — add deployment manifest/redaction regression tests.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation; every write targeted exactly `UnknownGod2011/NVIDEA` and no other repository was mutated.
- Official Nebius MysteryBox documentation confirms secrets have versions and that a `version_id` can select a specific version, while omission uses the primary version. This supports treating explicit versions as pinned/reproducible references.
- Static review confirms the new manifest does not include raw serverless access tokens, S3 static credentials, project/subnet ids, raw bucket name, PEM text, secret ids/version ids, provider response bodies, or research data.
- Static review confirms the deployment fingerprint changes when an immutable image/topology/public key or redacted secret-reference commitment changes.
- This runtime still has no usable .NET SDK, so compilation and test execution are **not claimed**.
- No live Nebius credentials/resources were available, so Object Storage/Serverless/model execution is **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- Version pin reporting never resolves MysteryBox payloads.
- Raw MysteryBox identifiers are not emitted; SHA-256 commitments permit comparison across contract runs while reducing disclosure.
- Project and subnet ids remain deliberately outside the evidence manifest, keeping the compute description project-independent.
- Public-key fingerprints are computed from DER SubjectPublicKeyInfo, not PEM formatting, so harmless line-ending/format changes do not alter identity.
- Manifest generation reuses the exact existing Object Storage/Serverless alignment preflight rather than duplicating weaker topology rules.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; new code/tests are statically reviewed but not compiled/executed.
- The actual `Nvidea.NebiusContractProbe` environment loader still accepts only secret-id environment variables; it has not yet been wired to optional per-secret version-id variables or to emit the new redacted manifest/fingerprint during `--live-research-preflight`/`--live-research`.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- Exact provider acceptance of the configured Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the hidden worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the real contract succeeds.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Wire the new reproducibility layer into `Nvidea.NebiusContractProbe`: support optional `*_VERSION_ID` variables for all three worker MysteryBox secrets while retaining secret-id development compatibility; make `--live-research-preflight` emit only pin-status plus the redacted `DeploymentFingerprintSha256`; optionally persist the redacted manifest to an explicit user-selected path; and make a real `--live-research` PASS print the same fingerprint so the PASS is cryptographically tied to the preflighted immutable deployment. Then, when real resources are supplied, run preflight followed by the end-to-end contract and persist the exact redacted PASS/failure evidence.
