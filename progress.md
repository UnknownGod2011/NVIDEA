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
- Nebius Object Storage + Serverless-mounted worker transport share one protected protocol. Preflight validates mount alignment, READ_WRITE transport, MysteryBox references, immutable worker image, bounded resources, and separated RSA identities.
- Worker envelope, dispatch-signing, and client result-envelope RSA purposes are separated end-to-end with RSA >=2048, OAEP-SHA256 capability proofs, bounded PEM, canonicalization, and temporary-buffer zeroization.
- `NebiusResearchWorkerRuntimeConfiguration` is the single worker environment boundary; credential-free topology/model/timing validation occurs before private-key/provider-secret reads.
- Nebius Serverless lifecycle interpretation is explicitly allowlisted. Bounded `state_details` diagnostics are untrusted evidence only and never determine lifecycle transitions.
- Durable remote provenance contains optional structured `ProviderFailureCode`; `ProviderFailureCodeTrust` is the single bounded/control-character-free evidence-shape boundary and the field is valid only for `RemoteFailed` provenance.
- `NebiusFailureRemediationPolicy` is intentionally narrower than evidence validation. Only locally verified `NotEnoughResources` and `Quota` codes map to guidance; provider text cannot authorize actions.
- Generic job-handler/provider exception text crosses `JobFailureDiagnostic` before durable `LastError`, is bounded/normalized, and is excluded from append-only audit summaries.
- Product-facing exception failures cross `DesktopUiFailureProjector`, which ignores exception message/type/stack content and emits fixed NVIDEA-authored text.
- Runtime-provided desktop text crosses `DesktopDisplayTextTrust` where display is necessary: control/whitespace normalization, explicit bounds, fixed fallback, and privacy-reduced HTTP(S) targets that omit URL user-info/query/fragment data.
- Browser approval WPF labels page/runtime targets explicitly as untrusted and preserves exact NVIDEA-generated approval scope verbatim.
- Browser recovery WPF no longer renders arbitrary recovery detail or full evidence URLs. Browser outcome/status WPF uses trusted local state rather than raw runtime diagnostic strings.
- **Product-facing browser outcomes now cross `BrowserProductOutcomeTrust` in Core before leaving `BrowserProductRuntime`.** Failed/retry messages are fixed local text and cannot contain `LastError`; approval summary/target presentation is bounded/normalized and privacy-reduced while exact approval scope remains byte-for-byte unchanged.
- Windows voice invocation is local/review-first. Deterministic judging tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capabilities/approvals/audit, durable jobs, Playwright execution, Windows shell, DPAPI state protection, persistent browser sessions/downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence ranking/staleness/diversity, restart-safe research, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, immutable worker images, MysteryBox validation, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-12 — Product, evaluator, provider, and protocol hardening
Added research/browser product runtimes, WPF lifecycle integration, restart-safe browser recovery, one-shot cloud approval, local voice, semantic-memory migration UI, positive/adversarial Personal AI evaluators, unified judging evidence, demo-package validation, provider endpoint/redirect trust, credential-read ordering, worker/client RSA role validation, protocol-level worker/client envelope trust, and deployment/protocol policy reuse.

### 2026-09-12 — Client RSA key-purpose separation
Separated dispatch signing/verification from result encryption/decryption across live configuration, runtime composition, worker bootstrap/execution, contract probe, deployment preflight, tests, README, and Nebius operator docs. Worker/live/preflight reject reused identities; worker receives only public halves.

### 2026-09-13 — Serverless lifecycle + failure provenance
- Added exhaustive fail-closed Serverless lifecycle coverage.
- Added bounded `state_details.code/message`, duplicate-shape ambiguity rejection, and explicit untrusted diagnostic evidence.
- Added fixed local remediation for `NotEnoughResources` and `Quota` only.
- Added structured `RemoteResearchProvenance.ProviderFailureCode`, durable migration, CAS equivalence, product precedence, direct reconciler capture, and raw JSON -> parser -> reconciler -> CAS coverage.
- Unified raw provider code validation through `ProviderFailureCodeTrust` and enforced `RemoteFailed`-only state semantics.

### 2026-09-13 — Durable and desktop diagnostic taint boundaries
- Added `JobFailureDiagnostic`; arbitrary handler/provider exception text is bounded, normalized, marked untrusted, and removed from append-only audit summaries.
- Added `DesktopUiFailureProjector`; direct `Exception.Message` disclosure was removed from invocation, browser/recovery, download, and audit-retention WPF catch paths.
- Added `DesktopDisplayTextTrust`; browser/recovery/status/runtime display strings and privacy-sensitive URL presentation are constrained before WPF rendering.
- Hardened approval display without changing exact approval authority.

### 2026-09-13 — Browser product API outcome trust boundary
Completed in this run:
- Re-read this ledger and inspected current `BrowserHostRuntime`, `BrowserProductRuntime`, composition-root exposure, display-text trust, and existing browser integration tests before mutation.
- Confirmed the remaining product-boundary leak: trusted `BrowserHostRuntime.Describe(...)` still includes durable `job.LastError` in failed/retry `BrowserJobOutcome.Message`, and raw approval summary/target values exist before WPF projection.
- Added `BrowserProductOutcomeTrust` as a Core product-facing projection boundary. It replaces all browser status messages from trusted `AgentJobState`, never from incoming outcome text.
- Failed and retry-scheduled product messages can no longer contain `LastError`, provider diagnostics, local paths, credentials, tool output, or site text.
- Approval summary is bounded/control-normalized through `DesktopDisplayTextTrust`.
- Approval target is bounded; absolute HTTP(S) targets are privacy-reduced to scheme + host + path so URL user-info/query/fragment secrets cannot escape through the product API.
- Exact approval scope, action kind, job id, requested timestamp, state, and verified-step evidence remain unchanged; the projection has presentation authority only and cannot approve/retry/cancel/navigate/write.
- Updated every `BrowserProductRuntime` method returning `BrowserJobOutcome` (`StartActionAsync`, `ApproveAndResumeAsync`, `CancelAsync`) to apply the projection before returning to UI/plugin callers.
- Added `BrowserProductOutcomeTrustTests` covering fake Nebius `Quota`/provider diagnostics, bearer-like secrets, private Windows paths, oversized/control-character approval text, secret-bearing URL user-info/query/fragment removal, non-URL locator target normalization, exact-scope preservation, and verified-step preservation.

Engineering commits this run before this ledger update:
- `b2d7c69c1e6bc9430563e32edaf93f02861742a0` — add the browser product outcome trust boundary.
- `f55ac6018b573407f9fb0fcc899a2679418a1c5b` — apply outcome projection at `BrowserProductRuntime`.
- `991e3c1b372437b051dd02bf769fd58715c55b3f` — add adversarial browser product outcome trust regressions.

Validation / evidence this run:
- Verified before every GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`.
- Current repository tree before mutation was `a08cddbb616a9a10d235dc81aef7b28bfb966f38`.
- GitHub compare `a08cddbb616a9a10d235dc81aef7b28bfb966f38...991e3c1b372437b051dd02bf769fd58715c55b3f`: **3 commits ahead / 0 behind**.
- Compare shows exactly three intended files changed before this ledger update: added `BrowserProductOutcomeTrust.cs`, modified `BrowserProductRuntime.cs`, and added `BrowserProductOutcomeTrustTests.cs`.
- Static re-read confirms the public product browser boundary now projects all returned browser outcomes; the privileged host remains internal composition infrastructure and exact approval scope is untouched.
- `dotnet` and `csc` remain unavailable in this execution environment.
- **No compile, xUnit, WPF, Worker, evaluator, or live integration PASS is claimed.**
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

## Security / Privacy / Failure Review
- Provider failure messages remain non-authoritative; structured provider failure codes remain evidence-only and allowlist remediation remains local.
- Generic exception text cannot impersonate Nebius remote-failure evidence and is not copied into append-only audit summaries.
- WPF exception paths use fixed local projection; non-exception browser/recovery/status runtime strings are separately constrained.
- Browser product/plugin callers now receive a second Core projection layer even if they do not use WPF, eliminating reliance on UI-specific hygiene for `BrowserJobOutcome`.
- URL product display projection strips user-info/query/fragment to reduce accidental token/credential disclosure.
- Page/runtime approval text cannot alter exact NVIDEA-generated approval scope or approval decision logic.
- Existing encrypted transport, signed binding, cancellation/recovery, exact-once ingestion, browser safety, download quarantine, emergency stop, and permission UX were not removed or weakened.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler exists in this execution environment, so recent .NET/WPF/Worker changes require a real restore/build/test/run before compile confidence is justified.
- Real Windows execution remains mandatory before treating WPF voice/readiness/maintenance behavior and generated judging evidence as judge-ready.
- `BrowserHostRuntime.Describe(...)` still retains raw `LastError` in its **trusted internal** `BrowserJobOutcome` for internal goal/recovery infrastructure. Public product callers are now protected by `BrowserProductOutcomeTrust`, but trusted goal/recovery surfaces must continue to avoid rendering that internal message directly.
- `BrowserGoalSession.Detail` and `BrowserGoalVerifiedStep.VerificationDetail` can originate from planner/verifier/runtime evidence and deserve a dedicated product-display provenance audit before any new plugin/UI surface exposes them.
- Browser approval `ExactScope` may legitimately encode target-specific authority and is intentionally shown verbatim. Any future redesign must preserve exact human-visible authority rather than silently canonicalizing it.
- Provider catalogs can change; model listing does not prove quota, inference success, tool calling, context length, or every capability. A real Nebius inference smoke test remains required.
- Prompt-injection detection remains heuristic; capability gates and approvals remain mandatory defense-in-depth.
- Real `embeddinggemma` ranking quality still needs a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source` / `SourcePath` still requires a real job.
- Lower-level `NebiusResearchClientRuntime.Create(...)` still retains a same-key compatibility fallback for legacy unit/contract callers; production live composition and deployment preflight are stricter. Remove only after executable migration coverage exists.

## Single Best Next Task
Obtain a real .NET 8-capable Windows restore/build/test signal and fix every compile/runtime defect exposed by recent security/reliability migrations. If executable validation remains unavailable, audit and harden the **browser goal/recovery product presentation boundary** (`BrowserGoalSession.Detail`, `BrowserGoalVerifiedStep.VerificationDetail`, planner reasons, verification evidence, and recovery evidence) so site/model/tool-controlled strings are bounded, provenance-labelled where necessary, privacy-reduced, and never gain approval/replay/control-flow authority; add contract/adversarial tests before exposing those fields to future plugins or UI.
