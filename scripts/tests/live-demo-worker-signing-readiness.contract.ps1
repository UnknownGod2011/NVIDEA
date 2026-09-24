[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$readiness = Join-Path $repoRoot 'scripts/live-demo-readiness.ps1'
if (-not (Test-Path -LiteralPath $readiness -PathType Leaf)) { throw "Readiness script missing: $readiness" }

$text = Get-Content -LiteralPath $readiness -Raw -Encoding UTF8

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Require-Fragment([string]$Fragment, [string]$Message) {
    Assert-True ($text.Contains($Fragment, [StringComparison]::Ordinal)) $Message
}

# Network/provider-free source contract for the recording-day worker-origin trust boundary.
# Executable RSA behavior is intentionally left to PowerShell 7/.NET 8 qualification on Windows.
Require-Fragment "Test-ConfigPresence 'NVIDEA_LIVE_SECRET_WORKER_RESULT_SIGNING_PRIVATE_KEY_ID'" 'Worker signing MysteryBox secret ID must be mandatory.'
Require-Fragment "Test-ConfigPresence 'NVIDEA_LIVE_SECRET_WORKER_RESULT_SIGNING_PRIVATE_KEY_VERSION_ID'" 'Worker signing MysteryBox version pin must be mandatory.'
Require-Fragment "GetEnvironmentVariable('NVIDEA_WORKER_RESULT_VERIFICATION_PUBLIC_KEY_PEM')" 'Client worker-verification public pin must be loaded explicitly.'
Require-Fragment "BEGIN (?:RSA )?PRIVATE KEY" 'Readiness must reject accidental private-key PEM on the client.'
Require-Fragment '$rsa.ImportFromPem($pem)' 'Readiness must cryptographically parse the configured RSA identity.'
Require-Fragment '$rsa.KeySize -lt 2048' 'Readiness must reject weak RSA worker identities.'
Require-Fragment '$rsa.ExportSubjectPublicKeyInfo()' 'Fingerprint authority must derive from canonical public-only SPKI.'
Require-Fragment '[Security.Cryptography.SHA256]::HashData($spki)' 'Public worker identity fingerprint must use SHA-256 over canonical SPKI.'
Require-Fragment 'Test-WorkerResultSigningReadiness' 'Worker result-signing readiness function must remain in the recording path.'

$cloudBlock = [regex]::Match($text, '(?s)if \(\$RequireCloudResearch\) \{(?<body>.*?)\n\}')
Assert-True $cloudBlock.Success 'RequireCloudResearch fail-closed block is missing.'
Assert-True ($cloudBlock.Groups['body'].Value.Contains('Test-WorkerResultSigningReadiness', [StringComparison]::Ordinal)) 'Cloud recording readiness must invoke worker signing readiness.'

# Guard against the most damaging disclosure regressions. The readiness output may expose only the
# derived public fingerprint; opaque MysteryBox values and PEM text must never be interpolated/logged.
$writeHostLines = @($text -split "`r?`n" | Where-Object { $_ -match '\bWrite-Host\b' })
foreach ($line in $writeHostLines) {
    Assert-True (-not $line.Contains('$pem', [StringComparison]::OrdinalIgnoreCase)) 'Readiness output must never print worker verification PEM text.'
    Assert-True (-not $line.Contains('PRIVATE_KEY_ID', [StringComparison]::Ordinal)) 'Readiness output must never print a MysteryBox secret ID.'
    Assert-True (-not $line.Contains('PRIVATE_KEY_VERSION_ID', [StringComparison]::Ordinal)) 'Readiness output must never print a MysteryBox secret version reference.'
}
Require-Fragment 'public pin SHA-256=$fingerprint' 'Readiness should expose only the non-secret canonical public fingerprint as worker identity evidence.'

Write-Host 'live-demo worker-signing readiness contract PASS (network-free static trust-boundary checks).'
