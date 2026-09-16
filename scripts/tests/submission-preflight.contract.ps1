[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$preflight = Join-Path $repoRoot "scripts/submission-preflight.ps1"
if (-not (Test-Path -LiteralPath $preflight -PathType Leaf)) { throw "Preflight script missing: $preflight" }

$text = Get-Content -LiteralPath $preflight -Raw

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Index-Of([string]$Needle) {
    $index = $text.IndexOf($Needle, [StringComparison]::Ordinal)
    Assert-True ($index -ge 0) "Expected preflight contract fragment is missing: $Needle"
    return $index
}

# This is deliberately network-free: it validates the orchestration source contract rather than
# invoking dotnet or any provider. Executable integration remains the Windows recording-machine gate.
$validator = Index-Of 'tools/Nvidea.DemoPackageValidator/Nvidea.DemoPackageValidator.csproj'
$generator = Index-Of 'tools/Nvidea.DemoChecklistGenerator/Nvidea.DemoChecklistGenerator.csproj'
$positive = Index-Of 'tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj'
$adversarial = Index-Of 'tools/Nvidea.PersonalAiAdversarialEval/Nvidea.PersonalAiAdversarialEval.csproj'

Assert-True ($validator -lt $generator) "Validator must run before checklist generation."
Assert-True ($generator -lt $positive) "Checklist generation must run before positive evaluation."
Assert-True ($positive -lt $adversarial) "Positive evaluation must run before adversarial evaluation."

Assert-True ($text.Contains('$LASTEXITCODE -ne 0')) "Child-process non-zero exits must fail closed."
Assert-True ($text.Contains('ArtifactsDirectory must remain inside the repository.')) "Artifact path confinement guard is missing."
Assert-True ($text.Contains('StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)')) "Artifact path confinement must compare the canonical repository prefix."

# The default zero-cost path must never gain a provider-live project/command silently. Keep this
# denylist narrow and executable-command oriented so explanatory safety text remains allowed.
$forbiddenProjectFragments = @(
    'Nvidea.JudgingEvidenceVerifier.csproj',
    'NebiusLive',
    'TavilyLive',
    'PlaywrightLive'
)
foreach ($fragment in $forbiddenProjectFragments) {
    Assert-True (-not $text.Contains($fragment)) "Default preflight contains forbidden provider-live command fragment: $fragment"
}

Assert-True ($text.Contains('Provider-live checks skipped (zero-cost/fail-closed mode).')) "Explicit provider-live skip disclosure is missing."

Write-Host "submission-preflight contract PASS (network-free static orchestration checks)."
