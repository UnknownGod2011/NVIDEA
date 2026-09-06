# NVIDEA Hackathon Progress

## Mission
Build a competition-grade, open-source Personal AI edition inspired by keyboard.wtf for the Nebius x NVIDIA Global AI Hackathon. The project must preserve the strongest desktop-assistant ideas while materially upgrading the intelligence/runtime into an NVIDIA/Nebius-first agent system with durable memory, research, complex browser automation, skills, permissioning, verification, and long-running execution.

Target track: Personal AI.
Secondary prize target: Best Use of Tavily.
Overall ambition: top-three / Grand Prize quality, judged as a complete product rather than a model wrapper.

## HARD REPOSITORY BOUNDARY
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material. Never commit, edit, delete, open PRs/issues, change settings, rerun workflows, or otherwise mutate it.
- Never write to ANY other repository.
- Before every GitHub mutation, verify the repository target is exactly `UnknownGod2011/NVIDEA`.
- Do not delete existing NVIDEA functionality merely to simplify implementation; migrate/refactor carefully and keep working paths unless a tested replacement exists.

## Required Work Loop
Every automation run must:
1. Read this file before doing anything else.
2. Inspect the current NVIDEA repo state and recent changes.
3. Choose the highest-value unfinished engineering task that improves the actual product and hackathon score.
4. Research current official Nebius/NVIDIA/Tavily docs when an API/model/platform assumption could be stale.
5. Implement real code/config/tests/docs in NVIDEA; do not spend a run only planning when a safe implementation is possible.
6. Test/validate as far as available tooling permits; never claim unverified behavior works.
7. Review the change for security, privacy, permissions, failure modes, Windows UX, and hackathon-rule fit.
8. Update this file at the end with what changed, evidence/tests, unresolved risks, and the next best task.
9. Continue on the next hourly run. Do not declare the project 'done' while meaningful quality, reliability, UX, testing, architecture, or demo improvements remain.

## Baseline: keyboard.wtf strengths to reuse conceptually/code-wise where license/structure permits
- Windows .NET desktop app with global hotkeys and voice orb.
- Local Vosk/Whisper speech paths.
- Jarvis mode with allow-listed tools and confirmation gates.
- Active-app / selected-text / clipboard context.
- App/file/folder resolution, local aliases, workflows, action history.
- Browser tab controls and safe desktop actions.
- Local encrypted secrets and privacy-conscious behavior.
- Existing destination integrations and installer/release structure.

## Baseline weaknesses that MUST be materially solved
### Memory
Current keyboard.wtf memory is intentionally tiny/simple: explicit key/value entries, 20-entry cap, substring search, short prompt digest. Replace this with a layered personal-memory system rather than merely increasing the cap.

Desired memory layers:
- working/session memory
- episodic interaction/action memory
- semantic/profile/preferences memory
- project/entity memory
- reusable workflow/skill memory
- retrieval with semantic relevance + recency + importance
- provenance, timestamps, confidence, user edit/delete controls
- privacy boundaries and retention controls
- compaction/summarization to control context and cost
- memory write policy that avoids saving junk or sensitive content unintentionally

### Research
Add a proper research subsystem using Tavily where appropriate:
- search -> source collection -> extraction -> synthesis -> citation/provenance
- multi-query planning for non-trivial questions
- stale-information detection
- source quality ranking and deduplication
- tool-result grounding and explicit uncertainty
- resumable research tasks for long jobs

### Browser automation
Current browser capability is mostly tab/navigation/search control and explicitly lacks full DOM reading/form automation. Build a real browser agent layer with safe, auditable execution.

Desired capabilities:
- browser session abstraction
- DOM/accessibility-tree-first inspection rather than coordinate-only clicking
- navigation, read, click, type, select, upload/download when permissioned
- robust selector strategy and retries
- page-state verification after actions
- structured observations for the reasoning model
- plan -> act -> observe -> verify loop
- timeout/cancellation/recovery
- authentication/session boundaries; never bypass login, CAPTCHA, OS/browser permissions, or site safeguards
- explicit approval for high-impact actions (send, submit, purchase, delete, publish, financial/account changes)
- audit log / screenshots or evidence references where appropriate
- anti-prompt-injection/tool-output trust boundaries

## Target Architecture
### Desktop shell
Keep a fast Windows-first interaction surface: hotkeys, voice/text, orb/status, local context, permission UX.

### Agent core
- task planner/state machine
- structured tool calling
- iterative plan/act/observe/verify
- interruption/cancel
- retries and bounded autonomy
- explicit completion criteria
- human-in-the-loop approvals

### NVIDIA/Nebius backend
The hackathon runtime must genuinely depend on NVIDIA open models served through Nebius Token Factory and/or Nebius AI Cloud.
- route lightweight intents/extraction to a fast Nemotron tier when available
- route general tool reasoning to an appropriate Nemotron agent/reasoning model
- route hard/long-context planning/coding/research to the strongest appropriate Nemotron model available in current Nebius docs
- central provider abstraction, retries, timeout, rate-limit handling, structured-output validation, telemetry and cost/latency observations
- avoid retaining Gemini/OpenAI/Claude as hidden primary reasoning paths in the hackathon edition; optional compatibility adapters may exist only if clearly non-default and not required for core judging flows

### Nebius execution
Use Nebius serverless/cloud components where they add real value, especially for long-running/background jobs. Keep local execution for OS-private actions that must remain on the user's machine.

### Skills
Build a capability/skill registry with declared:
- name/version
- tools required
- data permissions
- risk class
- confirmation requirements
- inputs/outputs
- test/evaluation fixtures

Candidate skills: research, browser task, email drafting, calendar preparation, coding/repo help, file summarization, workflow execution.

### Security / personal AI contract
- least privilege
- local secrets encryption
- no silent destructive actions
- approval policy based on action risk
- prompt-injection defenses for web/tool content
- audit trail
- clear local-vs-cloud data disclosure
- emergency stop
- no secret/API-key commits

## Hackathon product bar
Every major feature should strengthen at least one judging dimension:
- Technological implementation: real agent architecture and substantive Nebius/NVIDIA use.
- Design: coherent end-to-end desktop experience, not a debug console.
- Potential impact: a credible personal AI that saves real work across the computer.
- Quality of idea: personal operating layer, not another chat wrapper.

Demo should eventually prove, in <=3 minutes:
1. natural invocation from anywhere on Windows;
2. context understanding;
3. durable memory influencing a later task;
4. non-trivial Tavily-backed research with sources;
5. multi-step browser task with visible plan/verification;
6. permission gate before a consequential action;
7. long-running/background task where Nebius infrastructure is meaningful;
8. concise architecture view showing Nemotron/Nebius/Tavily as core dependencies.

## Engineering priorities
1. Import/adapt only the useful keyboard.wtf foundation into NVIDEA without touching the source repo.
2. Establish clean solution/package architecture and build/test baseline.
3. Replace primary AI backend with Nebius Token Factory + NVIDIA Nemotron abstraction.
4. Implement real layered memory + retrieval + tests.
5. Implement research subsystem + Tavily + citations/provenance + tests.
6. Implement browser agent/runtime with safe action/observation/verification loop.
7. Implement skills, risk/permission engine, audit trail.
8. Add background/resumable job execution and Nebius serverless integration where justified.
9. Integrate all of the above into the desktop UX.
10. Reliability: integration tests, mocks, fixtures, cancellation, retries, offline/error states.
11. Security/privacy review and threat model.
12. Packaging, onboarding, sample config, public demo environment.
13. Hackathon README, architecture diagrams, setup, license, attribution, changes-since-Aug-26 documentation.
14. Demo scenario, demo data, deterministic fallback, and final judging audit.

## Current State
- NVIDEA repository began empty.
- This progress ledger is the first project artifact.
- keyboard.wtf has been inspected read-only for product baseline.
- No production implementation has yet been ported into NVIDEA.

## Progress Log
### 2026-09-06 — Initialization
- Established immutable repository-safety boundary.
- Captured keyboard.wtf strengths and known gaps.
- Defined target Personal AI architecture and hackathon judging bar.
- Defined memory, research, browser-automation, skills, security, Nebius/NVIDIA and demo workstreams.
- Next highest-value task: inspect keyboard.wtf architecture read-only, create an explicit port/refactor map, then establish a buildable NVIDEA foundation with the smallest useful set of copied/adapted components and tests.
