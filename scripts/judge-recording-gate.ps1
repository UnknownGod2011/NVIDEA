[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$BrowserEvidenceDirectory,

    [ValidateRange(1, 168)]
    [int]$BrowserEvidenceMaxAgeHours = 24,

    [switch]$SkipBuildValidation,
    [switch]$SkipVerifierRegression
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
$verifierRegressionScript = Join-Path $PSScriptRoot 'test-browser-qualification-verifier.ps1'
if (-not (Test-Path -LiteralPath $readinessScript -PathType Leaf)) {
    throw 'live-demo-readiness.ps1 is missing; judge recording is blocked.'
}
if (-not $SkipVerifierRegression -and -not (Test-Path -LiteralPath $verifierRegressionScript -PathType Leaf)) {
    throw 'Browser qualification verifier regression harness is missing; judge recording is blocked.'
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

# Both subordinate scripts intentionally own process exit semantics. Execute them in
# child PowerShell processes so this recording-specific wrapper independently observes
# and enforces each status rather than allowing a nested exit to bypass the final gate.
$pwsh = (Get-Command pwsh -ErrorAction Stop).Source

if (-not $SkipVerifierRegression) {
    Write-Host 'Self-testing the browser qualification verifier before trusting retained evidence...'
    & $pwsh '-NoLogo' '-NoProfile' '-File' $verifierRegressionScript
    $verifierRegressionExitCode = $LASTEXITCODE
    if ($verifierRegressionExitCode -ne 0) {
        throw "Judge recording gate failed because the browser qualification verifier regression harness exited with code $verifierRegressionExitCode. Do not trust browser qualification evidence or record this checkout."
    }
} else {
    Write-Warning 'Verifier regression was explicitly skipped. This override is for diagnostics only and weakens the recording gate.'
}

$childArgs = @(
    '-NoLogo', '-NoProfile', '-File', $readinessScript,
    '-RequireCloudResearch',
    '-BrowserEvidenceDirectory', $resolvedEvidence.Path,
    '-BrowserEvidenceMaxAgeHours', [string]$BrowserEvidenceMaxAgeHours
)
if (-not $SkipBuildValidation) {
    $childArgs += '-ValidateBuild'
} else {
    Write-Warning 'Build validation was explicitly skipped. This override is for diagnostics only and should not be used for the final judge recording.'
}

& $pwsh @childArgs
$readinessExitCode = $LASTEXITCODE
if ($readinessExitCode -ne 0) {
    throw "Judge recording gate failed because live demo readiness exited with code $readinessExitCode. Do not record or release this checkout."
}

Write-Host 'NVIDEA judge recording gate PASS. Verifier self-test, browser qualification evidence, cloud readiness, and default build validation were enforced unless an explicit diagnostic override was supplied.'
exit 0
