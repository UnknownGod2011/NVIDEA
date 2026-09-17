[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$preflightPath = Join-Path $repoRoot 'scripts/submission-preflight.ps1'
$verifierPath = Join-Path $repoRoot 'scripts/verify-preflight-receipt.ps1'

function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Assert-Contains([string]$Text, [string]$Needle, [string]$Message) { Assert-True ($Text.Contains($Needle, [StringComparison]::Ordinal)) $Message }

$preflight = Get-Content -LiteralPath $preflightPath -Raw -Encoding UTF8
$verifier = Get-Content -LiteralPath $verifierPath -Raw -Encoding UTF8

# Production PASS must remain downstream of independent verification. This is deliberately
# source-level and network-free: behavioral tamper cases below belong to the Windows suite.
$verifyMarker = "& `$receiptVerifier -RepositoryRoot `$root -ReceiptPath `$receipt"
$passMarker = "Submission preflight PASS (local zero-cost scope)."
$verifyIndex = $preflight.IndexOf($verifyMarker, [StringComparison]::Ordinal)
$passIndex = $preflight.IndexOf($passMarker, [StringComparison]::Ordinal)
Assert-True ($verifyIndex -ge 0) 'Production preflight no longer invokes the independent receipt verifier.'
Assert-True ($passIndex -gt $verifyIndex) 'Production PASS marker must remain strictly after receipt verification.'

# Do not permit a catch-and-continue region around the verifier invocation.
$between = $preflight.Substring($verifyIndex, $passIndex - $verifyIndex)
Assert-True (-not $between.Contains('catch', [StringComparison]::OrdinalIgnoreCase)) 'Verifier-to-PASS region must not catch and suppress verification failures.'
Assert-Contains $preflight '$ErrorActionPreference = "Stop"' 'Preflight must retain terminating-error semantics.'

# The independent verifier must continue to re-read and bind the files rather than trusting
# receipt metadata. These assertions protect the tamper classes that matter to the PASS gate.
foreach ($needle in @(
    'Get-FileHash',
    'Get-Item',
    'providerLiveEvidence',
    "'local-zero-cost'",
    'manifest',
    'artifacts',
    'sha256',
    'length',
    'GetFullPath',
    'GetRelativePath'
)) {
    Assert-Contains $verifier $needle "Receipt verifier lost required tamper defense token: $needle"
}

# Ensure receipt verification cannot be silently made optional through an environment switch,
# force flag, or permissive parameter on the production preflight.
$parameterHeader = $preflight.Substring(0, [Math]::Min($preflight.Length, 800))
foreach ($forbidden in @('SkipReceipt', 'SkipVerification', 'DisableVerification', 'TrustReceipt', 'ForcePass')) {
    Assert-True (-not $parameterHeader.Contains($forbidden, [StringComparison]::OrdinalIgnoreCase)) "Production preflight exposes forbidden verification bypass: $forbidden"
}

# The receipt itself must remain explicitly scoped as local/non-provider-live before verification.
Assert-Contains $preflight "scope='local-zero-cost'" 'Receipt must retain local-zero-cost scope.'
Assert-Contains $preflight 'providerLiveEvidence=$false' 'Receipt must explicitly deny provider-live evidence.'

Write-Host 'preflight receipt tamper contract PASS (network/provider free).'
