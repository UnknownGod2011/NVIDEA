[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$ArtifactsDirectory = "artifacts/preflight"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-RepositoryFilePath {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$RepositoryPrefix)
    $fullPath = [IO.Path]::GetFullPath($Path)
    if (-not $fullPath.StartsWith($RepositoryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Expected output must remain inside the repository: $fullPath"
    }
    return $fullPath
}

function Invoke-CheckedDotnet {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][string]$ExpectedOutput,
        [Parameter(Mandatory)][string]$RepositoryPrefix
    )

    $output = Assert-RepositoryFilePath -Path $ExpectedOutput -RepositoryPrefix $RepositoryPrefix
    # Never let a stale artifact satisfy this run. A successful child must materialize fresh evidence.
    Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue

    Write-Host "==> $Label"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Label failed with exit code $LASTEXITCODE." }
    if (-not (Test-Path -LiteralPath $output -PathType Leaf)) {
        throw "$Label returned success but did not materialize expected output: $output"
    }
    $item = Get-Item -LiteralPath $output
    if ($item.Length -le 0) {
        throw "$Label returned success but materialized an empty output: $output"
    }
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
    Invoke-CheckedDotnet -Label "Validate schema-v2 demo package" -ExpectedOutput $validation -RepositoryPrefix $rootPrefix -Arguments @(
        "run", "--project", "tools/Nvidea.DemoPackageValidator/Nvidea.DemoPackageValidator.csproj", "--",
        ".", "docs/demo-package.json", "--output", $validation
    )

    # Checklist generation is intentionally downstream of validation. A malformed or unsafe
    # manifest can therefore never refresh the operator checklist used for recording.
    $checklist = Join-Path $artifacts "recording-checklist.md"
    Invoke-CheckedDotnet -Label "Generate validated recording checklist" -ExpectedOutput $checklist -RepositoryPrefix $rootPrefix -Arguments @(
        "run", "--project", "tools/Nvidea.DemoChecklistGenerator/Nvidea.DemoChecklistGenerator.csproj", "--",
        "docs/demo-package.json", "--output", $checklist
    )

    $positive = Join-Path $artifacts "personal-ai-positive.json"
    Invoke-CheckedDotnet -Label "Run positive Personal AI evaluator" -ExpectedOutput $positive -RepositoryPrefix $rootPrefix -Arguments @(
        "run", "--project", "tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj", "--", "--output", $positive
    )

    $adversarial = Join-Path $artifacts "personal-ai-adversarial.json"
    Invoke-CheckedDotnet -Label "Run adversarial Personal AI evaluator" -ExpectedOutput $adversarial -RepositoryPrefix $rootPrefix -Arguments @(
        "run", "--project", "tools/Nvidea.PersonalAiAdversarialEval/Nvidea.PersonalAiAdversarialEval.csproj", "--", "--output", $adversarial
    )

    Write-Host "==> Provider-live checks skipped (zero-cost/fail-closed mode)."
    Write-Host "    No Nebius catalog/live PASS, Tavily, browser, inference, or judging-verifier live-evidence command was invoked."
    Write-Host "    Run the documented provider-live evidence workflow deliberately when fresh credentials and a recording environment are available."

    Write-Host "Submission preflight PASS (local zero-cost scope)."
    Write-Host "Validated checklist: $checklist"
    Write-Host "Evidence: $positive ; $adversarial"
}
finally {
    Pop-Location
}
