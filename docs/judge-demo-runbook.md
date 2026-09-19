# NVIDEA <=3-minute judge demo runbook

This is the operator-facing companion to `docs/demo-package.json`. The JSON manifest remains the machine-checkable timing/claim boundary; this runbook turns each beat into a concrete product action, visible proof, dependency check, and safe fallback. **Milestone names in this document are exact production `SessionEvidenceKind` values and must remain identical to the schema-v2 manifest.**

## Non-negotiable claim boundary

Never describe synthetic evaluator output as live provider proof. Never claim a live Nebius Serverless execution unless authenticated/audited remote-result evidence has been observed for the demonstrated run and any separate provider PASS artifact being shown matches that deployment. Never expose API keys, cookies, tokens, raw checkpoints, private browser/session data, or provider exceptions on screen.

Before recording, use **New demo session** in the Judge Evidence dialog and confirm the reset. This clears only process-local judge milestones; it does not erase durable memory, jobs, browser state, authentication, downloads, or the audit trail.

## Preflight — do not start the recording until these pass

1. Build `Nvidea.Core`, `Nvidea.Windows`, and `Nvidea.Worker` on the Windows demo machine.
2. Run the positive and adversarial evaluator suites and the demo-package validator.
3. Run `./scripts/test-browser-qualification-verifier.ps1` under PowerShell 7. Treat any verifier regression failure as a release blocker.
4. From the exact clean commit that will be recorded, run `./scripts/run-browser-integration.ps1 -InstallChromium -SecuritySuite -KeepResults`. Retain the successful evidence directory; do not reuse evidence from another commit or a dirty checkout.
5. Run `./scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild -BrowserEvidenceDirectory <retained-browser-evidence-directory>`. This is the recording gate: it delegates to `verify-release-browser-gate.ps1`, which requires the expected NVIDEA GitHub origin, exact clean current HEAD, canonical browser security suite, intact TRX/receipt evidence, and evidence no older than 24 hours by default. **Do not record if this command fails.**
6. Confirm the Windows shell can invoke from the global hotkey and the emergency stop is reachable.
7. Confirm Nebius Token Factory/Nemotron readiness without displaying credentials.
8. Confirm Tavily is configured and a research request returns source-backed results.
9. Confirm the chosen browser demo site/session is already authenticated if authentication is required; never automate login, CAPTCHA, MFA, or other safeguards.
10. Confirm the browser scenario has a deterministic consequential final action that pauses for exact-scope approval.
11. If demonstrating live Nebius background execution, verify the redacted live PASS artifact before recording. Otherwise explicitly present the background path as an implemented contract/readiness path, not a completed live provider run.
12. Open the Judge Evidence dialog, start a New demo session, and verify all session milestones are initially absent.

The retained browser evidence directory is qualification evidence, not a portable credential or a substitute for the checkout. Keep it off-screen during the demo. If source code changes after qualification, rerun the browser suite and readiness gate from the new clean HEAD; the release gate intentionally rejects evidence bound to a different commit.

If any live dependency fails preflight, do not improvise a stronger claim. Use the fallback noted below and preserve the evidence-class distinction.

## 168-second deterministic sequence

### 0:00–0:20 — Invoke anywhere + bounded context

**Action:** Focus a normal Windows application with a short, non-sensitive selection. Invoke NVIDEA through the global hotkey and ask a concise question about the selected content.

**Visible result:** The NVIDEA shell appears over the active application, identifies the bounded active-app/selection context, and responds through the Nemotron-backed path. Do not silently include clipboard content.

**Judge proof:** Open/refresh Judge Evidence only if it can be done without derailing pacing; `NemotronInferenceCompleted` should be established only after successful inference.

**Fallback:** If the global hook fails, stop the take and fix it. Do not substitute a terminal-only invocation for the Windows-first claim.

### 0:20–0:42 — Durable memory influences a later answer

**Action:** Ask a follow-up whose answer depends on a previously stored, non-sensitive preference/project fact. Make the dependence obvious in the prompt without restating the remembered value.

**Visible result:** The response uses the retrieved memory and the UI indicates memory influence/provenance without revealing unrelated stored data.

**Judge proof:** `MemoryInfluencedResponse` should appear only when retrieved memory actually influenced the generated response.

**Fallback:** If semantic retrieval misses, use a known deterministic demo memory already stored through the product. Do not inject evidence manually or restate the answer in the prompt.

### 0:42–1:10 — Tavily-backed research with sources

**Action:** Start a compact current-information research request that benefits from multiple sources.

**Visible result:** Research shows source provenance/citations and explicit uncertainty where appropriate. Open at least one citation/source mapping so the judge sees that cited claims map to collected evidence.

**Judge proof:** `TavilyValidatedCitationUsed` should be established only after a validated Tavily citation is actually used.

**Fallback:** If Tavily is unavailable, the live Tavily beat has failed. Synthetic evaluator output may be shown later only as synthetic engineering evidence; it does not satisfy this beat and must not be described as live Tavily execution.

### 1:10–1:38 — Complex browser act-observe-verify

**Action:** Launch the prepared multi-step browser task. Use a scenario with at least one navigation/read step and one state-changing step before the final consequential action.

**Visible result:** The agent acts from DOM/accessibility observations, then visibly verifies post-action state rather than assuming a click succeeded. Keep the browser and NVIDEA status visible enough to make the act-observe-verify loop legible.

**Judge proof:** `BrowserVerifiedGoalCompleted` requires terminal goal completion through the trusted verified browser path; failed verification must halt/recover rather than manufacture success.

**Fallback:** If authenticated browser state has expired or trusted post-action verification cannot complete, stop the take and restore the session manually outside the recording. Never bypass login/CAPTCHA/MFA/site safeguards or manually complete the action and claim agent success.

### 1:38–1:58 — Consequential approval gate

**Action:** Let the same browser plan reach its send/submit/publish-style final action.

**Visible result:** NVIDEA pauses before the consequential mutation and presents the exact scope for approval. Briefly show that the action is still pending, then approve it deliberately.

**Judge proof:** `ConsequentialApprovalGranted` is recorded only after the trusted-host approval boundary grants the exact requested scope; approval alone does not prove that the browser mutation later succeeded.

**Fallback:** If no approval dialog appears, stop the take. Never complete the consequential action manually and describe it as agent-gated.

### 1:58–2:26 — Resumable/background Nebius path

**Action:** Show a long-running research job transitioning to the Nebius background execution path and the UI/state that makes the work resumable.

**Visible result:** Local private OS authority stays on-device while the cloud-appropriate research stage uses the remote contract. If a real provider run has been preflighted, show only redacted provider evidence and the reconciled local result.

**Judge proof:** `NebiusBackgroundExecutionObserved` is valid only after authenticated remote-result ingestion reaches durable local `ResultApplied` with its pending audit obligation cleared. Dispatch, Running, cancellation, provider failure, or an unaudited crash window are not proof.

**Fallback:** Without authenticated/audited remote-result evidence, the live background-execution beat has failed. The implemented Serverless contract/readiness may be shown later as documentation evidence, but it does not satisfy `NebiusBackgroundExecutionObserved` and must not be presented as completed provider execution.

### 2:26–2:48 — Architecture + evidence close

**Action:** Open the Judge Evidence/architecture view.

**Visible result:** Point to Nemotron/Nebius as the reasoning/background stack, Tavily as research, local Windows/browser authority boundaries, and the session milestones accumulated during the take. Keep the explanation to one sentence per dependency.

**Judge proof:** The evidence view is a payload-free projection of production-observed milestones, not a manual checklist. Provider readiness and provider-live claims remain distinct. This architecture beat intentionally has no expected runtime session milestone.

**Fallback:** If a milestone expected from any preceding live beat is absent, do not narrate it as completed. Treat the missing milestone as a failed demo assertion and investigate before the final recording.

### 2:48–3:00 — Contingency

Reserve these 12 seconds for UI latency, source opening, approval reading, or a concise closing line. Do not add a new feature beat here.

## Post-take acceptance gate

Reject the recording and rerun if any of the following occurred: a required live beat was skipped; an expected session milestone was absent; a citation was not visibly grounded; browser verification was not visible; a consequential mutation happened without approval; live Nebius/Tavily execution was claimed without corresponding live evidence; a secret/private payload appeared; a login/CAPTCHA/MFA safeguard was bypassed; or the take exceeded 180 seconds.

For the six live product beats, the expected exact milestone sequence is:

`NemotronInferenceCompleted` → `MemoryInfluencedResponse` → `TavilyValidatedCitationUsed` → `BrowserVerifiedGoalCompleted` → `ConsequentialApprovalGranted` → `NebiusBackgroundExecutionObserved`.

A valid take should make the product claim and the evidence boundary agree. The strongest close is not “everything passed”; it is that NVIDEA can show which capabilities were genuinely observed in this session and refuses to upgrade weaker evidence into a stronger claim.