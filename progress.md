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
- Tavily Search + Extract research with canonical deduplication, evidence quality/freshness/diversity, provenance, untrusted-evidence handling, and validated citations.
- Durable staged research checkpoints make synthesis restart-safe without repeating Tavily retrieval.
- Protected local state uses Windows CurrentUser DPAPI by default, job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, emergency stop, and explicit crash recovery.
- Remote research has bidirectional encrypted payload transport, one-stage `NebiusResearchWorker`, two-phase `DispatchReserved -> Nebius Create -> remote-id attachment`, exact-once result ingestion, deterministic lifecycle reconciliation across bounded complete job listings, durable cancellation, terminal/result-expiry handling, mounted encrypted transport, non-root worker image, and a signed authoritative remote-ID binding protocol.
- Signed binding publication is wired into normal two-phase dispatch, crash-window reservation recovery, and dispatched reconciliation; terminal binding cleanup is race-safe and best effort after durable terminal state.
- `NebiusResearchClientRuntime` provides one client-only composition boundary from a single signing identity.
- Nebius Serverless job creation models `spec.volumes[]`; research dispatch carries configured shared transport mounts to the worker.
- `NebiusResearchDeploymentPreflight` verifies the live worker topology without resolving secret values: exact `NVIDEA_TRANSPORT_ROOT`/volume-path match, `READ_WRITE` transport, MysteryBox-backed Nebius/Tavily/worker-private-key credentials, and the client public key required for signed binding verification.
- `NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment` proves that the native client bucket/prefix and the Serverless mounted bucket/SourcePath resolve to the same protected research objects before any job is submitted.
- `NebiusResearchLiveRuntimeFactory` is the explicit production composition gate and runs deployment preflight before constructing the live remote runtime.
- Current Nebius Serverless lifecycle parsing recognizes preparation states `PROVISIONING`, `IMAGE_PULLING`, `STARTING`; active `RUNNING`; teardown states `CANCELLING`, `DELETING`; terminal `COMPLETED`, `FAILED`, `ERROR`, and `CANCELLED`. Unknown future states remain fail-closed.
- Native Windows-side S3-compatible protected transport exists: `NebiusObjectStorageClient` + `S3ProtectedResearchTransport` publish/read encrypted work items, signed bindings, and encrypted results directly through Nebius Object Storage.
- `tools/Nvidea.NebiusContractProbe --live-research` uses that native Object Storage transport on the client and the Serverless-mounted directory transport in the worker, with exact bucket/prefix/mount alignment.
- `tools/Nvidea.NebiusContractProbe --live-research-preflight` is now a zero-cost local gate for the exact live configuration. It performs no provider/storage/model calls and rejects mutable worker image tags, malformed/weak RSA material, client signing/public-key mismatch, malformed compute/storage fields, plaintext worker credentials, and S3/mount misalignment before cloud submission.
- The real `--live-research` path executes the same zero-cost preflight before constructing provider clients or creating a Serverless job.
- Production remains truthfully local until the live Nebius/Object Storage/MysteryBox/container end-to-end probe actually succeeds.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable verified downloads, crash recovery, and single-owner browser state.

### 2026-09-09 — Research quality + durability
Added Tavily Extract enrichment, deterministic evidence quality/staleness/diversity handling, staged research boundaries, restart-safe synthesis, privacy-safe status, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership.

### 2026-09-09 — Nebius Serverless privacy/control plane
Added current Jobs REST contracts, MysteryBox secret refs, encrypted opaque-ID dispatch, protected result return, durable remote provenance, exact CAS result ingestion, two-phase dispatch reservation, deterministic crash reconciliation, typed provider lifecycle parsing, durable cancellation, bounded pagination, terminal/result-expiry reconciliation, mounted encrypted transport, non-root worker image, signed authoritative resource-ID bindings, automatic binding publication after durable remote-ID attachment, and terminal binding cleanup.

### 2026-09-09 — Live deployment preflight + probe
Added `NebiusServerlessVolumeMount`, strict mount validation, dispatcher passthrough, `NebiusResearchDeploymentPreflight`, `NebiusResearchLiveRuntimeFactory`, current provider-state handling, and `Nvidea.NebiusContractProbe --live-research` for the complete three-stage durable research workflow. The probe remains opt-in and no live PASS is claimed without real infrastructure.

### 2026-09-10 — Native Nebius Object Storage transport
Added `IProtectedResearchObjectStoreClient`, `NebiusObjectStorageClient`, and `S3ProtectedResearchTransport`; preserved independent `work-items/`, `dispatch-bindings/`, and `results/` namespaces; added create-once conditional S3 writes, 4 MiB bounds, explicit retries/timeouts, sanitized errors, and regression coverage. Added AWS SDK for .NET v4 and documented the least-privilege Object Storage security boundary.

### 2026-09-10 — Native S3 live-probe integration
Added exact client-S3 ↔ Serverless-volume bucket/prefix mapping validation and switched `--live-research` from a host-mounted client directory to native Object Storage. The Windows/client side now publishes encrypted work items/bindings/results directly via S3 while the worker consumes the same bucket prefix through its mounted directory view.

### 2026-09-10 — Current run: zero-cost live research deployment preflight
Completed:
- Re-read `progress.md` completely and inspected the current contract probe, deployment preflight, Object Storage transport, Serverless job model, recent commits, and existing preflight tests before changing anything.
- Refreshed current official Nebius Serverless documentation. It continues to document container images in `registry/path:tag` or `registry/path@digest` form; NVIDEA intentionally requires the digest form for the competition/live path to prevent image drift between validation and execution.
- Added `NebiusResearchLiveDryRunPreflight` in `src/Nvidea.Core/Jobs`.
- The dry-run gate reuses `NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment` and additionally validates bounded Serverless project/access-token inputs, compute/preset/subnet/timeout/disk shape, Object Storage endpoint/credentials/retry bounds, digest-pinned worker image identity, RSA key parseability/minimum size, and exact correspondence between the local client signing private key and the public verification key injected into the worker.
- The dry-run does not resolve MysteryBox values. Worker `NEBIUS_API_KEY`, `TAVILY_API_KEY`, and `NVIDEA_WORKER_PRIVATE_KEY_PEM` remain reference-only and plaintext forms still fail closed through the existing deployment preflight.
- Added regression tests for a valid digest-pinned aligned deployment, mutable image rejection, client signing/public-key substitution rejection, malformed worker public key rejection, and Object Storage prefix mismatch rejection.
- Added `--live-research-preflight` to `Nvidea.NebiusContractProbe`. It reads the same required deployment inputs as the real live path, runs only local parsing/validation, prints only non-sensitive PASS categories, and performs no Nebius Serverless, Token Factory, Tavily, or Object Storage request.
- `--live-research` now invokes that exact zero-cost preflight before constructing Object Storage/Serverless clients, so the paid/live path cannot bypass image/RSA/topology validation.
- Updated `docs/nebius-contract-probe.md` to document all three probe modes, the digest-pinned image requirement, RSA/signing-identity checks, zero-cost guarantees, required configuration, and the distinction between a preflight PASS and a real end-to-end PASS.

Commits this run:
- `ca6e7bf6103b34f505b84e81f0527db2ec5f009f` — add zero-cost live research deployment preflight.
- `a51b046b1c0d7e897fafe88c97f3bf9b22d34fc4` — add dry-run preflight regression tests.
- `19fcf814b200c2a5e6d75f9e626795bc08fd6df6` — add `--live-research-preflight` and gate the real live path through it.
- `5f9b3b224bd5fe9c36020e5244e97bf54aeecdc3` — document zero-cost preflight and immutable deployment requirements.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation and every write targeted exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Current official Nebius Serverless documentation checked during this run states that job images may be referenced by tag or digest. The NVIDEA live preflight deliberately accepts only `@sha256:<64-hex>` to make the judging/demo worker immutable.
- Static review confirms `--live-research-preflight` creates no `HttpClient`, `NebiusObjectStorageClient`, `NebiusServerlessJobClient`, model client, job record, or remote work item.
- Static review confirms the real `--live-research` calls the dry-run gate before constructing provider clients.
- Static review confirms RSA private/public correspondence is compared using exported SubjectPublicKeyInfo bytes with `CryptographicOperations.FixedTimeEquals`.
- Static review confirms the preflight never prints access tokens, S3 credentials, PEM contents, MysteryBox identifiers, bucket/object keys, provider bodies, or research payloads.
- This runtime still has no usable .NET SDK (`dotnet` unavailable), so compilation and test execution are **not claimed**.
- No live Nebius Object Storage credentials/bucket, immutable registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job are available here, so no real cloud PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- Mutable worker tags are now rejected on the explicit live path, reducing supply-chain/config drift between preflight and execution.
- Worker envelope public key and client signing private key must be valid RSA keys of at least 2048 bits; the configured client verification key must exactly match the signing private key.
- The worker private key remains MysteryBox-backed and is not resolved by the dry run, so the client never gains access to that server-side secret merely to validate deployment.
- Object Storage continues to receive encrypted work/result envelopes and signed control-plane bindings rather than research plaintext.
- Prefix/bucket mismatch still fails before cloud compute is created.
- The local contract-run Object Storage static credentials remain an explicit local-only credential boundary and are never injected into the worker.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; the new dry-run and tests are statically reviewed but not compiled/executed.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- The exact provider acceptance of the configured Serverless Object Storage `Source`/`SourcePath` still requires a real job; local topology validation cannot prove provider-side mount behavior.
- The dry run can validate the worker public key and that a worker-private-key MysteryBox reference exists, but it intentionally cannot prove that the hidden MysteryBox private key corresponds to that public key without resolving the secret. The real worker protocol remains the authoritative proof.
- Live Object Storage static credentials currently enter the probe via environment variables; this is suitable only for an explicit local contract run with a least-privilege short-lived shell/secret-injection mechanism, not a future polished end-user credential UX.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the live end-to-end contract succeeds.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Make the first real infrastructure-backed run maximally deterministic without weakening secret boundaries: add optional **MysteryBox version-pinned references** for the three worker secrets (while keeping secret-id compatibility for development), surface whether each live secret is version-pinned in the zero-cost preflight, and produce a redacted deployment fingerprint/manifest containing the worker image digest, project-independent compute topology, bucket/prefix mapping, public-key fingerprints, and secret-reference *types* (never values). Then, when credentials/resources are supplied, run `--live-research-preflight` followed by the real `--live-research` contract and persist the exact PASS/failure evidence without exposing secrets.
