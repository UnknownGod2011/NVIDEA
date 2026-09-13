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
- Runtime-provided desktop text now also crosses `DesktopDisplayTextTrust` where display is necessary: control/whitespace normalization, explicit bounds, fixed fallback, and privacy-reduced HTTP(S) targets that omit URL user-info/query/fragment data.
- Browser approval UI labels page/runtime targets explicitly as untrusted and bounds summary/target rendering; the exact NVIDEA-generated approval scope remains verbatim so the human sees the actual authority string.
- Browser recovery UI no longer renders arbitrary recovery detail or full evidence URLs. It uses fixed local recovery language and a privacy-reduced evidence target.
- Browser outcome UI is now projected solely from trusted `AgentJobState`; it no longer renders `BrowserJobOutcome.Message`, so durable/runtime diagnostics cannot leak into the Windows shell or become UI authority.
- Desktop session status UI is projected solely from `DesktopAgentState`; arbitrary `DesktopAgentStatus.Detail` is not rendered.
- Active-application/window-title context and interrupted-goal labels are bounded and normalized before rendering.
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
- Added structured `RemoteResearchProvenance.ProviderFailureCode`, durable migration, CAS equivalence, product precedence, direct reconciler capture, and raw JSON → parser → reconciler → CAS coverage.
- Unified raw provider code validation through `ProviderFailureCodeTrust` and enforced `RemoteFailed`-only state semantics.

### 2026-09-13 — Durable and desktop diagnostic taint boundaries
- Added `JobFailureDiagnostic`; arbitrary handler/provider exception text is bounded, normalized, marked untrusted, and removed from append-only audit summaries.
- Added `DesktopUiFailureProjector`; direct `Exception.Message` disclosure was removed from invocation, browser/recovery, download, and audit-retention WPF catch paths.
- Added adversarial tests proving fake Nebius failure evidence, bearer-like secrets, and private paths do not cross those exception presentation boundaries.

### 2026-09-13 — Non-exception desktop runtime-text hardening
Completed in this run:
- Re-read this ledger and current desktop/browser implementation before mutation.
- Added `DesktopDisplayTextTrust` with bounded single-line canonicalization and fixed fallbacks for runtime-provided display text.
- Added privacy-reduced navigation target projection: only HTTP(S) scheme + IDN host + path are retained; URL user-info, query, and fragment are intentionally omitted because they frequently carry credentials/tokens.
- Hardened `ApprovalDialog`: summary and target are bounded/normalized; target is visibly labelled `Untrusted browser target`; exact approval scope remains verbatim because changing the displayed authority string would be unsafe.
- Hardened browser-result rendering in `MainWindow`: product text is now selected only from trusted `AgentJobState`, so WPF no longer renders `BrowserJobOutcome.Message` or embedded `LastError` diagnostics.
- Hardened ambiguous recovery rendering: arbitrary `recovery.Detail` is no longer shown; full evidence URLs are replaced with privacy-reduced targets and fixed local recovery language.
- Hardened desktop status rendering: arbitrary `DesktopAgentStatus.Detail` is no longer shown; text is selected only from `DesktopAgentState`.
- Hardened active-app/window-title and interrupted browser-goal labels with bounded/control-normalized display projection.
- Added `DesktopDisplayTextTrustTests` covering control/newline/tab/NUL normalization, output bounds, missing-text fallback, secret-bearing URL user-info/query/fragment removal, and non-HTTP(S) fail-closed behavior.
- During static review, caught that the full-file WPF edit accidentally removed the `vk` argument from the `RegisterHotKey` P/Invoke signature; corrected it immediately in commit `497c8c610d73f480db4bdbed055f16ff4fed667a` before completing the run.

Engineering commits this run before this ledger update:
- `d6b7589c2e7a15b856b55af9c459b2f65fcf2e11` — add bounded desktop display-text trust primitive.
- `c0c561e8b27bbc6d66b5a9e47f52deb781a0d8a2` — harden browser approval display text and provenance labelling.
- `0195d0d136021b6c8663767f5632155663659e50` — apply runtime display projection to WPF surfaces (contained the transient P/Invoke signature regression).
- `497c8c610d73f480db4bdbed055f16ff4fed667a` — restore the correct four-argument `RegisterHotKey` P/Invoke signature.
- `ec67bf04ee10185d67f78b912820e5dfa07f296e` — add adversarial display-text trust tests.
- `b385ada829445555f7a63b9007e61bf27093775a` — stop rendering browser runtime diagnostic messages; project from trusted job state only.

Validation / evidence this run:
- Verified before every GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`.
- GitHub compare from prior ledger head `99f9d129c354d106faff0b845006b52686937e0d` to engineering head `b385ada829445555f7a63b9007e61bf27093775a`: **6 commits ahead / 0 behind**.
- Compare shows only four intended files changed before this ledger update: `DesktopDisplayTextTrust.cs`, `ApprovalDialog.xaml.cs`, `MainWindow.xaml.cs`, and `DesktopDisplayTextTrustTests.cs`.
- Static review confirms browser outcome WPF no longer consumes `BrowserJobOutcome.Message`; recovery WPF no longer consumes `recovery.Detail` or full URL query/fragment/user-info; status WPF no longer consumes `DesktopAgentStatus.Detail`.
- Static review confirms approval authority is unchanged: exact scope is still rendered verbatim and the same exact scope is echoed to `ApproveAndResumeAsync`; the new text helper has no approval/retry/browser/file/cloud/billing authority.
- `dotnet` and `csc` remain unavailable in this execution environment.
- **No compile, xUnit, WPF, Worker, evaluator, or live integration PASS is claimed.**
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

## Security / Privacy / Failure Review
- Provider failure messages remain non-authoritative; structured provider failure codes remain evidence-only and allowlist remediation remains local.
- Malformed or semantically inconsistent structured failure codes fail closed rather than falling back to legacy text.
- Generic exception text cannot impersonate Nebius remote-failure evidence and is not copied into append-only audit summaries.
- WPF exception paths use fixed local projection; non-exception browser/recovery/status runtime strings are now separately constrained.
- URL display projection intentionally strips user-info/query/fragment to reduce accidental token/credential disclosure.
- Page/runtime approval text is explicitly marked untrusted; it cannot alter the exact NVIDEA-generated approval scope or approval decision logic.
- Existing encrypted transport, signed binding, cancellation/recovery, exact-once ingestion, browser safety, download quarantine, emergency stop, and permission UX were not removed or weakened.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler exists in this execution environment, so recent .NET/WPF/Worker changes require a real restore/build/test/run before compile confidence is justified.
- Real Windows execution remains mandatory before treating WPF voice/readiness/maintenance behavior and generated judging evidence as judge-ready.
- `BrowserHostRuntime.Describe(...)` still constructs `BrowserJobOutcome.Message` using `job.LastError` for failed/retry states. The Windows shell no longer renders that field, but other future product/plugin callers could. The product API itself should be hardened next so safe presentation does not depend on every caller remembering the WPF rule.
- Browser approval `ExactScope` may legitimately encode target-specific authority and is intentionally shown verbatim. Any future redesign must preserve exact human-visible authority rather than silently canonicalizing it.
- Provider catalogs can change; model listing does not prove quota, inference success, tool calling, context length, or every capability. A real Nebius inference smoke test remains required.
- Prompt-injection detection remains heuristic; capability gates and approvals remain mandatory defense-in-depth.
- Real `embeddinggemma` ranking quality still needs a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source` / `SourcePath` still requires a real job.
- Lower-level `NebiusResearchClientRuntime.Create(...)` still retains a same-key compatibility fallback for legacy unit/contract callers; production live composition and deployment preflight are stricter. Remove only after executable migration coverage exists.

## Single Best Next Task
Obtain a real .NET 8-capable Windows restore/build/test signal and fix every compile/runtime defect exposed by recent security/reliability migrations. If executable validation remains unavailable, harden the **product API boundary** in `BrowserHostRuntime.Describe(...)` / `BrowserProductRuntime` so failed/retry `BrowserJobOutcome.Message` can never contain `LastError`, and move approval-summary/target projection into Core before the prompt reaches any UI/plugin caller; then add contract tests proving no raw provider/tool/site diagnostic escapes through the product runtime.
