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
- .NET 8 core in `src/Nvidea.Core`, WPF Windows host in `src/Nvidea.Windows`, deployable remote worker in `src/Nvidea.Worker`.
- NVIDIA Nemotron through Nebius Token Factory with retries, cancellation/timeouts, structured tool calling, response-schema support, and Nano/Super/Ultra routing.
- Layered personal memory with privacy-aware writes, provenance, semantic/recency/importance retrieval, local Ollama embeddings, migration/re-indexing, and maintenance UX.
- Tavily Search + Extract research with multi-query planning, canonical deduplication, evidence quality/freshness/diversity ranking, citations/provenance, resumable checkpoints, and explicit untrusted-evidence handling.
- Safe Playwright browser agent with persistent Chromium state, popup tracking, plan-act-observe-verify, prompt-injection detection, consequential-action approvals, quarantined downloads, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default; job state uses durable CAS, leases, and hash-chained/segmented audit.
- Remote research uses encrypted opaque work items, signed resource-ID bindings, two-phase dispatch, lifecycle reconciliation, durable cancellation, exact-once result ingestion, and race-safe cleanup.
- Nebius Object Storage + Serverless-mounted worker transport share one protected protocol. Preflight validates mount alignment, READ_WRITE transport, MysteryBox references, immutable worker image, bounded resources, and RSA identities.
- Worker envelope keys and client result-envelope keys use bounded PEM, RSA >=2048, strict role separation, OAEP-SHA256 capability proofs, canonicalization, and temporary-buffer zeroization. Dispatch-signing and result-envelope RSA purposes are separated end-to-end.
- `NebiusResearchWorkerRuntimeConfiguration` is the single worker environment boundary; credential-free topology/model/timing validation occurs before private-key/provider-secret reads.
- Nebius Serverless lifecycle interpretation is explicitly allowlisted. Bounded `state_details` diagnostics are untrusted evidence only and never determine lifecycle transitions.
- Durable remote provenance contains optional structured `ProviderFailureCode`. Newly verified remote failures place the parsed code into terminal provenance before CAS, so immediate and persisted records agree.
- `ProviderFailureCodeTrust` is the single evidence-shape trust boundary for provider failure codes: maximum 128 characters, surrounding-whitespace canonicalization, control-character rejection, unknown-code preservation, and no remediation authority.
- Provider diagnostic `message` remains separately bounded to 1024 characters and is display/audit evidence only; it is never promoted into structured failure classification.
- `ProviderFailureCode` is valid only when `RemoteResearchProvenance.State == RemoteFailed`; inconsistent structured failure metadata is rejected at durable boundaries.
- `NebiusFailureRemediationPolicy` remains narrower than evidence validation. Only verified local allowlist entries currently map to guidance: `NotEnoughResources` and `Quota`. Provider messages and unknown codes cannot authorize retry/resubmit/cancel, resize resources, change projects, spend money, or cause any other side effect.
- Generic job-handler/provider exception text crosses `JobFailureDiagnostic` before durable `LastError`: detail is bounded to 768 characters, control/whitespace characters are normalized, and an NVIDEA-owned `Execution error (untrusted):` prefix prevents generic exceptions from impersonating legacy Nebius remote-failure evidence.
- Generic exception text is no longer copied into append-only audit summaries; retry/exhaustion transitions remain driven only by exception occurrence and attempt count.
- Product-facing WPF exception failures now cross `DesktopUiFailureProjector`, which intentionally ignores exception message/type details and emits fixed local text for invocation, browser execution/recovery/discovery, download quarantine/recovery/export/discard, and audit-retention inspection.
- Windows voice invocation is local/review-first. Deterministic judging tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capabilities/approvals/audit, durable jobs, Playwright execution, Windows shell, DPAPI state protection, persistent browser sessions/downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence ranking/staleness/diversity, restart-safe research, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, immutable worker images, MysteryBox validation, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-12 — Product, evaluator, provider, and protocol hardening
Added research/browser product runtimes, WPF lifecycle integration, restart-safe browser recovery, one-shot cloud approval, local voice, semantic-memory migration UI, positive/adversarial Personal AI evaluators, unified judging evidence, demo-package validation, provider endpoint/redirect trust, credential-read ordering, worker/client RSA role validation, protocol-level worker/client envelope trust, and deployment/protocol policy reuse.

### 2026-09-12 — Client RSA key-purpose separation + deployment enforcement
Separated dispatch signing/verification from result encryption/decryption across live configuration, runtime composition, worker bootstrap/execution, contract probe, deployment preflight, tests, README, and Nebius operator docs. Worker/live/preflight reject reused identities; worker receives only public halves.

### 2026-09-13 — Serverless lifecycle, diagnostics, and structured failure provenance
- Audited Serverless lifecycle vocabulary and added exhaustive fail-closed lifecycle coverage.
- Added bounded `state_details.code/message` parsing, duplicate-shape ambiguity rejection, generic fallback, and explicit `Provider diagnostic (untrusted)` evidence.
- Added fixed local remediation for `NotEnoughResources` and `Quota`; provider messages never select guidance or actions.
- Added optional `RemoteResearchProvenance.ProviderFailureCode`, durable migration from recognized legacy failure evidence, CAS equivalence over the structured field, and product UI precedence for structured provenance.
- Lifecycle reconciliation places parsed failure classification into terminal provenance before CAS, keeping returned and persisted records consistent.
- Added `ProviderFailureCodeTrust` and enforced the state invariant that structured failure codes are valid only on `RemoteFailed` provenance.
- Unified raw `stateDetails.code` / `state_details.code` parsing with `ProviderFailureCodeTrust`; unknown bounded codes remain evidence-only.
- Added raw JSON → parser → reconciler → CAS persistence coverage for provider failure-code canonicalization.

### 2026-09-13 — Generic durable diagnostic taint boundary
- Audited `ResearchJobStatus`, `NebiusFailureRemediationPolicy`, `ResearchProductUiState`, `ResumableJobOrchestrator`, and audit persistence for remaining `LastError`/provider-text authority.
- Added `JobFailureDiagnostic` so arbitrary handler exception text is bounded, normalized, and visibly marked untrusted before durable persistence.
- Removed raw handler/provider exception text from append-only audit summaries while preserving retry/exhaustion semantics.
- Added adversarial coverage showing a fake Nebius `Quota` failure embedded in a generic exception cannot become remediation or trusted audit evidence.

### 2026-09-13 — Desktop exception privacy boundary
Completed in this run:
- Re-read `progress.md` completely and inspected the current repository/head before changes.
- Audited WPF/product-facing catch paths in `MainWindow.xaml.cs`, `MainWindow.Research.cs`, `MainWindow.Memory.cs`, `MainWindow.Voice.cs`, `MainWindow.Downloads.cs`, `MainWindow.Readiness.cs`, `MainWindow.Audit.cs`, and startup handling.
- Confirmed research, memory, voice, readiness, and startup paths already used fixed/redacted user-facing failure text.
- Found direct `Exception.Message` disclosure in main desktop invocation, browser execution, ambiguous browser recovery, recovery discovery, download quarantine snapshot/recovery/export/discard, and audit-retention inspection.
- Added `DesktopUiFailureProjector` in `Nvidea.Core.Desktop`. The projector accepts the exception only as a required failure signal and intentionally never reads or returns its message/type/stack/payload. Each supported product surface maps to fixed NVIDEA-authored user/status text.
- Routed the identified WPF catches through that projection boundary while preserving cancellation behavior, approval boundaries, no-replay guarantees, and download least-privilege semantics.
- Added adversarial tests using a fake Nebius `Quota` failure, bearer secret, and private Windows path; all supported projections are required not to disclose those tokens or the original exception message.
- Unknown projection surfaces fail closed with `ArgumentOutOfRangeException` instead of falling back to exception text.

Engineering commits before this ledger update:
- `2ddc9243da56708796ae989d149eff204b5f425d` — add privacy-safe desktop failure projection.
- `2ca630a2469b8004d26055a4bd3432f1bc143dd4` — add adversarial projection tests.
- `00318baebc4f522adab27070ac35a7af189cc329` — remove raw exception disclosure from invocation/browser/recovery UI.
- `c58806c53c0a1f2ebc0dc16ba8c58029c0207ab9` — extend projection to download surfaces.
- `8579d3dcd56be11412faf8b82d9e8efbd890bf46` — cover download projections in privacy tests.
- `f98d22c9c1b8809df29f95363f119d0ac78897a2` — remove raw exception disclosure from download UI.
- `831b3fb1bf2169e8601a99397bfcf3d53408cffd` — extend projection to audit viewer.
- `6c0d42091297862ee6127cc8160d8d8f510bbe31` — cover audit projection in privacy tests.
- `f8d4d0b9676850e4fe933797b933a27a841296db` — remove raw exception disclosure from audit-retention UI.

Validation / evidence this run:
- Verified before every GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`.
- GitHub compare from prior ledger head `f1e4cf53ebb994a77067550258a4f35d60a0be36` to engineering head `f8d4d0b9676850e4fe933797b933a27a841296db`: **9 commits ahead / 0 behind**.
- Compare shows only five intended files changed before this ledger update: `DesktopUiFailureProjection.cs`, `MainWindow.xaml.cs`, `MainWindow.Downloads.cs`, `MainWindow.Audit.cs`, and `DesktopUiFailureProjectorTests.cs`.
- Static review confirms exception text no longer enters those product UI catch paths; fixed guidance retains browser/download safety instructions without provider/tool/local payload disclosure.
- Static review confirms cancellation paths remain distinct and unchanged; no consequential action, retry, replay, export, discard, or approval is authorized by the new projection layer.
- `dotnet` and `csc` are unavailable in this execution environment; focused tests were committed but could not be executed here.
- **No compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.**
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

## Security / Privacy / Failure Review
- Provider failure `message` remains non-authoritative and is not promoted into structured provenance or product guidance.
- `ProviderFailureCode` remains classification evidence only. Unknown/future bounded codes cannot trigger retries, resubmission, cancellation, resizing, project changes, billing actions, or other side effects.
- Structured failure metadata has both shape and state invariants: it must be bounded/control-character-free and belong to `RemoteFailed` provenance.
- Malformed or semantically inconsistent structured codes fail closed rather than falling back to legacy text.
- Legacy Nebius migration remains intentionally allowlist-only; generic exception text carries a disjoint NVIDEA-owned prefix and cannot impersonate that evidence format.
- Generic handler/provider diagnostics remain available only in bounded durable troubleshooting state and do not enter append-only audit summaries.
- WPF exception projection now suppresses raw exception messages across the identified desktop/browser/download/audit catch surfaces, preventing accidental display of provider payloads, credentials, local file paths, prompts, or forged failure evidence.
- `DesktopUiFailureProjector` does not classify exceptions or select actions from exception content. It is presentation-only and has no retry, approval, browser, file, cloud, or billing authority.
- Existing encrypted transport, authenticated associated data, signed binding, cancellation/recovery, exact-once ingestion, Tavily/Nemotron behavior, browser safety, download quarantine, and Windows permission UX were not removed or weakened.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler exists in this execution environment, so recent .NET/WPF/Worker changes still require a real restore/build/test/run before they can be considered compile-verified.
- Real Windows execution remains mandatory before treating WPF voice/readiness/maintenance behavior and generated judging evidence as judge-ready.
- Provider catalogs can change; model listing does not prove quota, inference success, tool calling, context length, or every capability. A real Nebius inference smoke test remains required.
- Prompt-injection detection remains heuristic; capability gates and approvals remain mandatory defense-in-depth.
- Real `embeddinggemma` ranking quality still needs a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source` / `SourcePath` still requires a real job.
- Only `NotEnoughResources` and `Quota` are allowlisted because those are the failure classifications verified from first-party evidence. Do not guess additional action classifications without current provider evidence or a real contract capture.
- Lower-level `NebiusResearchClientRuntime.Create(...)` still retains a same-key compatibility fallback for legacy unit/contract callers; production live composition and deployment preflight are stricter. Remove only after executable migration coverage exists.
- Non-exception product text still needs a separate taint review: `DesktopAgentStatus.Detail`, browser outcome/recovery detail strings, filenames/URLs, and other runtime-provided display fields may be legitimate user-visible data but should be checked for provenance, bounds, control characters, and accidental authority before calling the desktop disclosure audit complete.

## Single Best Next Task
Obtain a real .NET 8-capable Windows restore/build/test signal and fix every compile/runtime defect exposed by recent security/reliability migrations. If executable validation remains unavailable, perform the next desktop taint audit over non-exception runtime-provided display strings (`DesktopAgentStatus.Detail`, browser outcome/recovery details, download/browser metadata) and introduce bounded/provenance-aware projections where those values can contain provider/tool/site-controlled or private data.
