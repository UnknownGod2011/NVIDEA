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
    if (-not $fullPath.StartsWith($RepositoryPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Expected output must remain inside the repository: $fullPath" }
    return $fullPath
}

function Read-StrictEvidenceJson {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$Label)
    $item = Get-Item -LiteralPath $Path
    if ($item.Length -gt 1MB) { throw "$Label output exceeds the 1 MiB semantic-verification limit." }
    try { return (Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json -ErrorAction Stop) }
    catch { throw "$Label produced invalid JSON evidence: $($_.Exception.Message)" }
}

function Assert-PassedEvidence {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$Label, [Parameter(Mandatory)][int]$ExpectedSchemaVersion)
    $evidence = Read-StrictEvidenceJson -Path $Path -Label $Label
    if ($null -eq $evidence.schemaVersion -or [int]$evidence.schemaVersion -ne $ExpectedSchemaVersion) { throw "$Label produced an unexpected evidence schema version." }
    if ($null -eq $evidence.overallPassed -or $evidence.overallPassed -isnot [bool] -or -not $evidence.overallPassed) { throw "$Label produced evidence that is not an explicit PASS." }
    if ($null -eq $evidence.checks -or @($evidence.checks).Count -eq 0) { throw "$Label produced no machine-verifiable checks." }
    foreach ($check in @($evidence.checks)) {
        if ([string]::IsNullOrWhiteSpace([string]$check.id) -or $check.passed -isnot [bool] -or -not $check.passed) { throw "$Label contains a missing, malformed, or failed check." }
    }
    return $evidence
}

function Assert-ValidatorEvidence {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$ManifestPath)
    $evidence = Assert-PassedEvidence -Path $Path -Label "Demo package validator" -ExpectedSchemaVersion 2
    $manifest = Read-StrictEvidenceJson -Path $ManifestPath -Label "Demo manifest"
    if ($null -eq $manifest.beats -or [int]$evidence.beatCount -ne @($manifest.beats).Count) { throw "Validator evidence beat count does not match the canonical manifest." }
    if ([int]$evidence.plannedDurationSeconds -ne (@($manifest.beats) | Measure-Object -Property durationSeconds -Sum).Sum) { throw "Validator evidence duration does not match the canonical manifest." }
}

function Assert-ChecklistMatchesManifest {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$ManifestPath)
    $manifest = Read-StrictEvidenceJson -Path $ManifestPath -Label "Demo manifest"
    $text = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $matches = [regex]::Matches($text, '(?m)^##\s+(\d+)\.\s+(.+?)\s+\((\d+)s\)\s*$')
    if ($matches.Count -ne @($manifest.beats).Count) { throw "Generated checklist beat count does not match the canonical manifest." }
    for ($i = 0; $i -lt @($manifest.beats).Count; $i++) {
        $beat = @($manifest.beats)[$i]
        if ([int]$matches[$i].Groups[1].Value -ne ($i + 1) -or $matches[$i].Groups[2].Value -ne [string]$beat.label -or [int]$matches[$i].Groups[3].Value -ne [int]$beat.durationSeconds) { throw "Generated checklist beat $($i + 1) does not match the canonical manifest order/label/duration." }
        foreach ($milestone in @($beat.expectedSessionMilestones)) {
            if (-not $text.Contains("``$milestone``", [StringComparison]::Ordinal)) { throw "Generated checklist is missing expected milestone '$milestone'." }
        }
    }
    if (-not $text.Contains("## Take acceptance gate", [StringComparison]::Ordinal)) { throw "Generated checklist is missing its take acceptance gate." }
}

function Invoke-CheckedDotnet {
    param([Parameter(Mandatory)][string[]]$Arguments, [Parameter(Mandatory)][string]$Label, [Parameter(Mandatory)][string]$ExpectedOutput, [Parameter(Mandatory)][string]$RepositoryPrefix)
    $output = Assert-RepositoryFilePath -Path $ExpectedOutput -RepositoryPrefix $RepositoryPrefix
    Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue
    Write-Host "==> $Label"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Label failed with exit code $LASTEXITCODE." }
    if (-not (Test-Path -LiteralPath $output -PathType Leaf)) { throw "$Label returned success but did not materialize expected output: $output" }
    if ((Get-Item -LiteralPath $output).Length -le 0) { throw "$Label returned success but materialized an empty output: $output" }
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
    Invoke-CheckedDotnet -Label "Validate schema-v2 demo package" -ExpectedOutput $validation -RepositoryPrefix $rootPrefix -Arguments @("run", "--project", "tools/Nvidea.DemoPackageValidator/Nvidea.DemoPackageValidator.csproj", "--", ".", "docs/demo-package.json", "--output", $validation)
    Assert-ValidatorEvidence -Path $validation -ManifestPath $manifest

    $checklist = Join-Path $artifacts "recording-checklist.md"
    Invoke-CheckedDotnet -Label "Generate validated recording checklist" -ExpectedOutput $checklist -RepositoryPrefix $rootPrefix -Arguments @("run", "--project", "tools/Nvidea.DemoChecklistGenerator/Nvidea.DemoChecklistGenerator.csproj", "--", "docs/demo-package.json", "--output", $checklist)
    Assert-ChecklistMatchesManifest -Path $checklist -ManifestPath $manifest

    $positive = Join-Path $artifacts "personal-ai-positive.json"
    Invoke-CheckedDotnet -Label "Run positive Personal AI evaluator" -ExpectedOutput $positive -RepositoryPrefix $rootPrefix -Arguments @("run", "--project", "tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj", "--", "--output", $positive)
    Assert-PassedEvidence -Path $positive -Label "Positive Personal AI evaluator" -ExpectedSchemaVersion 1 | Out-Null

    $adversarial = Join-Path $artifacts "personal-ai-adversarial.json"
    Invoke-CheckedDotnet -Label "Run adversarial Personal AI evaluator" -ExpectedOutput $adversarial -RepositoryPrefix $rootPrefix -Arguments @("run", "--project", "tools/Nvidea.PersonalAiAdversarialEval/Nvidea.PersonalAiAdversarialEval.csproj", "--", "--output", $adversarial)
    Assert-PassedEvidence -Path $adversarial -Label "Adversarial Personal AI evaluator" -ExpectedSchemaVersion 1 | Out-Null

    Write-Host "==> Provider-live checks skipped (zero-cost/fail-closed mode)."
    Write-Host "    No Nebius catalog/live PASS, Tavily, browser, inference, or judging-verifier live-evidence command was invoked."
    Write-Host "    Run the documented provider-live evidence workflow deliberately when fresh credentials and a recording environment are available."
    Write-Host "Submission preflight PASS (local zero-cost scope)."
    Write-Host "Validated checklist: $checklist"
    Write-Host "Evidence: $positive ; $adversarial"
}
finally { Pop-Location }
