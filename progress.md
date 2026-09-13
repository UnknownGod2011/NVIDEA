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
- Remote research uses encrypted opaque work items, signed resource-ID bindings, two-phase dispatch, lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, Nebius Object Storage, and Serverless-mounted worker transport.
- Worker envelope, dispatch-signing, and result-envelope RSA purposes are separated end-to-end with RSA >=2048, OAEP-SHA256 capability proofs, bounded PEM, canonicalization, and temporary-buffer zeroization.
- Provider/model/site/tool text is non-authoritative across explicit trust boundaries including `ProviderFailureCodeTrust`, `JobFailureDiagnostic`, `DesktopUiFailureProjector`, `DesktopDisplayTextTrust`, `BrowserProductOutcomeTrust`, and `BrowserGoalEvidenceTrust`.
- Parent browser-goal state no longer retains duplicate `PendingAction`; recovery authority remains the durable child job id plus exact approval scope.
- Browser child jobs scrub executable browser-action checkpoints after safely terminal failed/non-in-flight-cancelled outcomes; retryable and ambiguous-running jobs retain only the material still required for retry/recovery verification.
- Executed-but-unverified browser actions use `AmbiguousJobExecutionException`; orchestrator state remains `Running`, automatic replay is blocked, and fresh verification is required.
- Browser exact approval scopes are data-minimal: capability id + stable action id + ordered permission names. Browser action values, upload paths, rationale, page URLs, untrusted source, and tool arguments are not part of the scope.
- Capability audit failure events no longer persist raw backend/provider/site exception messages; durable audit records use fixed local failure text while the original exception still propagates to the immediate caller.
- Windows voice invocation is local/review-first. Deterministic judging tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capabilities/approvals/audit, durable jobs, Playwright execution, Windows shell, DPAPI state protection, persistent browser sessions/downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence ranking/staleness/diversity, restart-safe research, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, immutable worker images, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-12 — Product/evaluator/protocol hardening
Added research/browser product runtimes, WPF lifecycle integration, restart-safe browser recovery, one-shot cloud approval, local voice, semantic-memory migration UI, positive/adversarial Personal AI evaluators, unified judging evidence, demo-package validation, provider endpoint/redirect trust, credential-read ordering, worker/client RSA role separation, protocol-level envelope trust, and deployment/preflight policy reuse.

### 2026-09-13 — Trust boundaries and browser exact-once/privacy hardening
- Added structured provider-failure provenance, bounded remediation, generic failure-diagnostic quarantine, privacy-safe desktop exception/display projection, constrained browser product outcomes, bounded/privacy-reduced browser-goal evidence, and safe legacy goal-state migration.
- Applied browser-goal evidence projection both durably and in-process so planner history cannot receive a more permissive surface after same-process execution.
- Removed duplicate parent `PendingAction` retention while preserving `PendingJobId` + exact scope recovery semantics.
- Added terminal browser checkpoint scrubbing for safely failed/cancelled jobs.
- Added explicit ambiguous execution handling so driver-reported execution with failed verification is never auto-retried.
- Preserved ambiguous in-flight cancellation checkpoints for recovery verification instead of trading side-effect safety for premature data deletion.

### 2026-09-13 — Capability approval/audit privacy hardening
Completed in this run:
- Re-read this ledger and audited browser approval-scope construction, capability policy, tool execution, and durable audit emission before mutation.
- Confirmed `CapabilityPermissionPolicy.BuildScope(...)` constructs browser approval authority only from `CapabilityId`, stable `ActionId`, and ordered `DataPermission` names. Browser typed values, upload paths, destination URLs, rationale, `UntrustedSource`, and tool arguments are not incorporated into approval scope.
- Therefore a separate scope fingerprint is **not** currently required for browser exact approval: the existing exact scope is already a privacy-minimized authority string while preserving byte-for-byte human approval equality.
- Found an adjacent higher-severity durable privacy gap: `CapabilityToolExecutor` wrote raw backend exception messages to `tool.execution_failed` audit summaries. Those messages can include provider/site text, credentials, private paths, page content, or forged control language.
- Replaced raw exception audit summaries with fixed NVIDEA-authored text: `Tool execution failed after authorization; backend diagnostic text was withheld from audit.` The original exception is still rethrown to the immediate caller, so runtime error semantics are preserved without durable leakage.
- Added `CapabilityAuditPrivacyTests` adversarial coverage proving backend exception secrets/private paths/forged approval text do not enter the failed audit event.
- Added adversarial coverage proving a high-risk browser invocation containing a typed password, private upload path, and token-bearing untrusted URL produces an approval scope containing only `browser.agent|<actionId>|FilesRead,BrowserWrite`; the same privacy-safe scope is what the waiting-for-approval audit event receives.
- Approval consumption, exact equality, one-shot grants, browser execution, retry/no-replay policy, and audit hash-chain/retention mechanics were not intentionally changed.

Engineering commits this run before this ledger update:
- `41110106d82421d8e65e7ee1edae43d2d32df5d5` — quarantine backend exception text from capability audit.
- `baddcbebda01a5bb41df4a120aa64f5dc2da2df0` — add adversarial capability audit/scope privacy regressions.

Validation / evidence this run:
- Verified immediately before every successful GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`.
- Starting repository head was `e90a2a7e06092e47dc9862beb305433a1c98e5ef`.
- GitHub compare before this ledger commit reported **2 commits ahead / 0 behind**, touching exactly `CapabilityToolExecutor.cs` and the new `CapabilityAuditPrivacyTests.cs`.
- Static review confirms exception propagation remains unchanged: only the durable audit summary was replaced with fixed local text.
- Static review confirms browser exact approval scope excludes invocation summary, untrusted source, and tool arguments by construction.
- Environment check again found no usable `dotnet`, `csc`, or `msbuild` executable.
- **No compile, xUnit, WPF, Worker, evaluator, or live integration PASS is claimed.** The new regressions are persisted but unexecuted here.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

## Security / Privacy / Failure Review
- Browser approval scopes are intentionally exact and byte-for-byte stable, but are now explicitly verified to contain no browser payload values or source URL data.
- Durable capability failure audit records no longer receive raw backend exception messages, preventing credentials/private paths/site text from becoming retained evidence or accidental UI authority.
- Executable browser action data remains only where pending/retry/ambiguous-recovery semantics require it; safe terminal records use fixed tombstones.
- Executed-but-unverified side effects cannot enter automatic retry through the generic failure path.
- Ambiguous execution/cancellation retains the checkpoint intentionally because fresh verification requires the intended action/postcondition; privacy minimization does not override side-effect safety.
- Exact approval comparison semantics and ephemeral single-use approval grants remain unchanged.
- Existing encrypted transport, signed binding, remote exact-once ingestion, browser download quarantine, emergency stop, and permission UX were not removed or weakened.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler exists in this environment, so recent Core/WPF/Worker changes still require a real restore/build/test/run before compile confidence is justified.
- Real Windows execution remains mandatory before treating WPF voice/readiness/maintenance behavior and generated judging evidence as judge-ready.
- Ambiguous `Running`/in-flight-cancelled browser records intentionally retain executable action material until recovery resolves the side effect. Cleanup must scrub it immediately after a trusted terminal reconciliation.
- Generic capability/action identifiers are still caller-supplied strings at the lower-level capability framework. The production browser path uses a fixed capability id and GUID action id, but other future capabilities should avoid placing user/provider content in those identity fields because they are part of exact approval and audit scope.
- `BrowserHostRuntime.Describe(...)` still retains raw `LastError` in its trusted internal outcome for goal/recovery infrastructure; public browser product callers and browser-goal presentation paths are constrained, but future trusted internal consumers must not render it directly.
- Provider catalogs can change; model listing does not prove quota, inference success, tool calling, context length, or every capability. A real Nebius inference smoke test remains required.
- Prompt-injection detection remains heuristic; capability gates and approvals remain mandatory defense-in-depth.
- Real embedding ranking and real Nebius Object Storage/Serverless execution still require live environment validation.
- Lower-level `NebiusResearchClientRuntime.Create(...)` retains a same-key compatibility fallback for legacy unit/contract callers; production composition/preflight are stricter. Remove only after executable migration coverage exists.

## Single Best Next Task
Obtain a real .NET 8-capable Windows restore/build/test signal and fix every compile/runtime defect exposed by recent migrations. If executable validation remains unavailable, harden the **generic capability identity/audit boundary**: validate/bound `CapabilityId`, `ActionId`, and tool-name audit metadata so future capabilities cannot accidentally place user/provider secrets or control characters in durable approval/audit identity fields, while preserving the fixed `browser.agent` + GUID browser authority contract.
