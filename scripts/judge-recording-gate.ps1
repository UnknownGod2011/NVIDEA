[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$BrowserEvidenceDirectory,

    [ValidateRange(1, 168)]
    [int]$BrowserEvidenceMaxAgeHours = 24,

    [switch]$SkipBuildValidation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($PSVersionTable.PSVersion.Major -lt 7) {
    throw 'PowerShell 7+ is required for the judge recording gate.'
}

if (-not $IsWindows) {
    throw 'The judge recording gate must run on Windows.'
}

$readinessScript = Join-Path $PSScriptRoot 'live-demo-readiness.ps1'
if (-not (Test-Path -LiteralPath $readinessScript -PathType Leaf)) {
    throw 'live-demo-readiness.ps1 is missing; judge recording is blocked.'
}

try {
    $resolvedEvidence = Resolve-Path -LiteralPath $BrowserEvidenceDirectory -ErrorAction Stop
} catch {
    throw 'Browser qualification evidence directory does not exist or is inaccessible.'
}

if (-not (Test-Path -LiteralPath $resolvedEvidence.Path -PathType Container)) {
    throw 'Browser qualification evidence must be a directory.'
}

Write-Host 'Running fail-closed NVIDEA judge recording gate...'
Write-Host 'This gate requires cloud-research configuration and fresh browser qualification evidence.'

# Readiness intentionally exits with a process status. Run it in a child PowerShell
# process so its status remains observable here and cannot bypass this wrapper's final
# recording decision.
$pwsh = (Get-Command pwsh -ErrorAction Stop).Source
$childArgs = @(
    '-NoLogo', '-NoProfile', '-File', $readinessScript,
    '-RequireCloudResearch',
    '-BrowserEvidenceDirectory', $resolvedEvidence.Path,
    '-BrowserEvidenceMaxAgeHours', [string]$BrowserEvidenceMaxAgeHours
)
if (-not $SkipBuildValidation) {
    $childArgs += '-ValidateBuild'
}

& $pwsh @childArgs
$readinessExitCode = $LASTEXITCODE
if ($readinessExitCode -ne 0) {
    throw "Judge recording gate failed because live demo readiness exited with code $readinessExitCode. Do not record or release this checkout."
}

Write-Host 'NVIDEA judge recording gate PASS. Browser qualification evidence was mandatory for this path.'
exit 0
