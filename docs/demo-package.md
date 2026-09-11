# Judge-ready demo package

`docs/demo-package.json` is the canonical <=3-minute NVIDEA judging plan. It is deliberately machine-checkable rather than a loose script.

## Goals

The package must show, in one coherent flow:

1. invocation anywhere on Windows with bounded desktop context;
2. durable memory affecting later behavior;
3. Tavily-backed research with source provenance and citation validation;
4. multi-step browser automation with post-action verification;
5. an explicit approval gate before a consequential action;
6. resumable/background research where the Nebius Serverless contract is meaningful; and
7. a closing architecture/evidence view proving Nemotron, Nebius, and Tavily are core dependencies while private OS actions stay local.

The current manifest budgets 168 seconds, leaving 12 seconds of contingency under the hard 180-second cap.

## Validate the package

From the repository root:

```powershell
dotnet run --project tools/Nvidea.DemoPackageValidator/Nvidea.DemoPackageValidator.csproj -- . docs/demo-package.json --output artifacts/demo-package-validation.json
```

The validator is offline and credential-free. It checks:

- schema version and strict JSON parsing;
- duplicate JSON-property rejection;
- unique/stable demo beat IDs;
- the required seven judging beats;
- aggregate timing <= the declared cap and <=180 seconds;
- that every referenced feature/evidence/project path exists inside the repository;
- required README/license/evaluator assets;
- that demo commands point at their declared project files;
- explicit evidence classes: `synthetic`, `local-live`, `provider-live`, or `documentation`;
- that a beat marked `requiresProviderLive=true` has at least one `provider-live` evidence reference; and
- a conservative manifest secret scan for common key/token forms.

The output includes the SHA-256 of the exact manifest bytes, so a recorded validation can be tied to the reviewed demo plan.

## Evidence classes are claims boundaries

`synthetic` means a deterministic fake/provider-edge harness exercised real NVIDEA Core contracts. It is useful engineering evidence but **not proof that an external provider was live**.

`local-live` means a real local runtime was exercised, for example a Windows/WPF/Playwright/Ollama path on the demo machine.

`provider-live` means the referenced artifact came from a real external provider execution. For Nebius, this should be the redacted deployment/PASS evidence path described in `docs/judging-evidence.md` and verified through `tools/Nvidea.JudgingEvidenceVerifier`.

`documentation` explains architecture or procedure and is never sufficient by itself to claim successful live execution.

The current `nebius-background` beat intentionally has `requiresProviderLive=false`: the repository has the Serverless contract, preflight, worker, redacted manifest, and evidence verifier, but this repository has not yet demonstrated a credential-backed live PASS. During a demo, do not say a live remote run succeeded unless that matching live PASS artifact was actually generated and verified. Once it is, update the manifest to require provider-live evidence and point the beat at the reviewed artifact/package path.

## Recommended evidence generation order

```powershell
# 1. Build/test first.
dotnet build .\src\Nvidea.Core\Nvidea.Core.csproj
dotnet build .\src\Nvidea.Windows\Nvidea.Windows.csproj
dotnet build .\src\Nvidea.Worker\Nvidea.Worker.csproj

# 2. Generate deterministic positive + adversarial evidence.
dotnet run --project tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj -- --output artifacts/personal-ai-positive.json
dotnet run --project tools/Nvidea.PersonalAiAdversarialEval/Nvidea.PersonalAiAdversarialEval.csproj -- --output artifacts/personal-ai-adversarial.json

# 3. Generate Nebius redacted preflight/PASS evidence only when the real deployment is intentionally available.
# See docs/nebius-contract-probe.md and docs/judging-evidence.md.

# 4. Combine synthetic and live provider evidence without conflating them.
dotnet run --project tools/Nvidea.JudgingEvidenceVerifier/Nvidea.JudgingEvidenceVerifier.csproj -- artifacts/personal-ai-positive.json artifacts/personal-ai-adversarial.json artifacts/nebius-manifest.json artifacts/nebius-pass.json --output artifacts/judging-summary.json

# 5. Validate the demo package itself.
dotnet run --project tools/Nvidea.DemoPackageValidator/Nvidea.DemoPackageValidator.csproj -- . docs/demo-package.json --output artifacts/demo-package-validation.json
```

## Recording checklist

Before the final <=3-minute recording, verify that the demo machine shows the same behavior the evidence claims. In particular: microphone consent is visible before recording; clipboard remains opt-in; memory sensitivity controls are not bypassed; research sources/citations are visible; the browser plan visibly verifies state after mutation; Submit/send-style actions stop for approval; emergency stop remains reachable; and no API key, cookie, token, private key, provider exception, raw checkpoint, or private browser/session data appears on screen.

A deterministic evaluator PASS is supporting evidence, not a substitute for showing the product. A live provider PASS is provider evidence, not proof that every Windows/UI/browser path works. The final demo should present each class accurately.
