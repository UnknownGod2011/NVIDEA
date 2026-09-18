[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$EvidenceDirectory,

    [string]$ExpectedCommit,
    [switch]$RequireCleanSource
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$evidence = (Resolve-Path -LiteralPath $EvidenceDirectory -ErrorAction Stop).Path
$receiptPath = Join-Path $evidence "qualification-receipt.json"
$trxPath = Join-Path $evidence "browser-integration.trx"

if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
    throw "Qualification receipt not found: $receiptPath"
}
if (-not (Test-Path -LiteralPath $trxPath -PathType Leaf)) {
    throw "Browser integration TRX not found: $trxPath"
}

$receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
if ([int]$receipt.schemaVersion -ne 1) {
    throw "Unsupported qualification receipt schemaVersion '$($receipt.schemaVersion)'."
}
if ([string]::IsNullOrWhiteSpace([string]$receipt.sourceCommit)) {
    throw "Qualification receipt has no sourceCommit. Release/judge evidence must come from a Git checkout."
}
if ($RequireCleanSource -and $receipt.sourceDirty -ne $false) {
    throw "Qualification receipt is not from a proven-clean checkout (sourceDirty=$($receipt.sourceDirty))."
}
if (-not [string]::IsNullOrWhiteSpace($ExpectedCommit) -and
    -not [string]::Equals([string]$receipt.sourceCommit, $ExpectedCommit.Trim(), [StringComparison]::OrdinalIgnoreCase)) {
    throw "Qualification source commit '$($receipt.sourceCommit)' does not match expected commit '$ExpectedCommit'."
}

[xml]$trx = Get-Content -LiteralPath $trxPath -Raw
$results = @($trx.TestRun.Results.UnitTestResult)
if ($results.Count -eq 0) {
    throw "TRX contains zero test results."
}
$nonPassed = @($results | Where-Object { [string]$_.outcome -ne "Passed" })
if ($nonPassed.Count -gt 0) {
    $summary = @($nonPassed | ForEach-Object { "'$([string]$_.testName)'=$([string]$_.outcome)" }) -join "; "
    throw "TRX contains non-passed results: $summary"
}

$trxPassedNames = @($results | ForEach-Object { [string]$_.testName } | Sort-Object)
$receiptPassedNames = @($receipt.passedTests | ForEach-Object { [string]$_ } | Sort-Object)
if ([int]$receipt.passedCount -ne $trxPassedNames.Count) {
    throw "Receipt passedCount '$($receipt.passedCount)' does not match TRX passed count '$($trxPassedNames.Count)'."
}
if ($receiptPassedNames.Count -ne $trxPassedNames.Count -or
    (Compare-Object -ReferenceObject $trxPassedNames -DifferenceObject $receiptPassedNames).Count -ne 0) {
    throw "Receipt passedTests do not exactly match the TRX PASS evidence."
}

$requiredFixtures = @($receipt.requiredFixtures | ForEach-Object { [string]$_ })
if ($receipt.securitySuite -eq $true) {
    if ($requiredFixtures.Count -eq 0) {
        throw "Security-suite receipt declares no required fixtures."
    }
    foreach ($requiredFixture in $requiredFixtures) {
        if (-not ($trxPassedNames | Where-Object { $_ -like "*$requiredFixture*" })) {
            throw "Required security fixture '$requiredFixture' has no PASS evidence in the TRX."
        }
    }
}

# Qualification metadata is intentionally payload-free. Reject unexpected top-level fields so a
# future producer cannot silently add browser/session payloads to judge-facing evidence without
# updating this verifier and its review contract.
$allowedReceiptFields = @(
    "schemaVersion", "validatedAtUtc", "sourceCommit", "sourceDirty", "dotnetSdk",
    "configuration", "securitySuite", "effectiveFilter", "passedCount",
    "requiredFixtures", "passedTests"
)
$unexpectedFields = @($receipt.PSObject.Properties.Name | Where-Object { $_ -notin $allowedReceiptFields })
if ($unexpectedFields.Count -gt 0) {
    throw "Qualification receipt contains unexpected field(s): $($unexpectedFields -join ', '). Refusing unreviewed evidence schema expansion."
}

$validatedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$receipt.validatedAtUtc, [ref]$validatedAt)) {
    throw "Qualification receipt validatedAtUtc is invalid."
}

Write-Host "Chromium qualification evidence verified."
Write-Host "  Commit: $($receipt.sourceCommit)"
Write-Host "  Clean source: $(-not [bool]$receipt.sourceDirty)"
Write-Host "  Passed tests: $($trxPassedNames.Count)"
Write-Host "  Security suite: $([bool]$receipt.securitySuite)"
Write-Host "  Validated UTC: $($validatedAt.ToUniversalTime().ToString('O'))"
