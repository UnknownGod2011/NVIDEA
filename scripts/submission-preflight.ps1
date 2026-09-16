[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$ArtifactsDirectory = "artifacts/preflight",
    [switch]$IncludeProviderLive
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-CheckedDotnet {
    param([Parameter(Mandatory)][string[]]$Arguments, [Parameter(Mandatory)][string]$Label)
    Write-Host "==> $Label"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Label failed with exit code $LASTEXITCODE." }
}

$root = [IO.Path]::GetFullPath($RepositoryRoot)
if (-not (Test-Path -LiteralPath $root -PathType Container)) { throw "Repository root does not exist: $root" }
$manifest = Join-Path $root "docs/demo-package.json"
if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) { throw "Demo manifest is missing: $manifest" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw ".NET 8 SDK is required for submission preflight." }

$artifacts = if ([IO.Path]::IsPathRooted($ArtifactsDirectory)) { [IO.Path]::GetFullPath($ArtifactsDirectory) } else { [IO.Path]::GetFullPath((Join-Path $root $ArtifactsDirectory)) }
$rootPrefix = $root.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $artifacts.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "ArtifactsDirectory must remain inside the repository." }
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null

Push-Location $root
try {
    $validation = Join-Path $artifacts "demo-package-validation.json"
    Invoke-CheckedDotnet -Label "Validate schema-v2 demo package" -Arguments @(
        "run", "--project", "tools/Nvidea.DemoPackageValidator/Nvidea.DemoPackageValidator.csproj", "--",
        ".", "docs/demo-package.json", "--output", $validation
    )

    # Checklist generation is intentionally downstream of validation. A malformed or unsafe
    # manifest can therefore never refresh the operator checklist used for recording.
    $checklist = Join-Path $artifacts "recording-checklist.md"
    Invoke-CheckedDotnet -Label "Generate validated recording checklist" -Arguments @(
        "run", "--project", "tools/Nvidea.DemoChecklistGenerator/Nvidea.DemoChecklistGenerator.csproj", "--",
        "docs/demo-package.json", $checklist
    )

    $positive = Join-Path $artifacts "personal-ai-positive.json"
    Invoke-CheckedDotnet -Label "Run positive Personal AI evaluator" -Arguments @(
        "run", "--project", "tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj", "--", "--output", $positive
    )

    $adversarial = Join-Path $artifacts "personal-ai-adversarial.json"
    Invoke-CheckedDotnet -Label "Run adversarial Personal AI evaluator" -Arguments @(
        "run", "--project", "tools/Nvidea.PersonalAiAdversarialEval/Nvidea.PersonalAiAdversarialEval.csproj", "--", "--output", $adversarial
    )

    if ($IncludeProviderLive) {
        Write-Warning "Provider-live verification is opt-in and may require credentials/network or incur provider usage. This script does not invoke it automatically. Run the documented Nebius live evidence commands deliberately, then Nvidea.JudgingEvidenceVerifier against those fresh artifacts."
    } else {
        Write-Host "==> Provider-live checks skipped by default (zero-cost/fail-closed mode)."
        Write-Host "    No Nebius catalog/live PASS, Tavily, browser, inference, or judging-verifier live-evidence command was invoked."
    }

    Write-Host "Submission preflight PASS (local zero-cost scope)."
    Write-Host "Validated checklist: $checklist"
    Write-Host "Evidence: $positive ; $adversarial"
}
finally {
    Pop-Location
}
