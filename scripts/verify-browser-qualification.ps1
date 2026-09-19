[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$EvidenceDirectory,
    [string]$ExpectedCommit,
    [switch]$RequireCleanSource,
    [switch]$RequireSecuritySuite
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$evidence = (Resolve-Path -LiteralPath $EvidenceDirectory -ErrorAction Stop).Path
$receiptPath = Join-Path $evidence "qualification-receipt.json"
$trxPath = Join-Path $evidence "browser-integration.trx"
if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw "Qualification receipt not found: $receiptPath" }
if (-not (Test-Path -LiteralPath $trxPath -PathType Leaf)) { throw "Browser integration TRX not found: $trxPath" }

# This list is intentionally independent of receipt metadata. Release/judge verification must not
# let the evidence producer redefine what "the security suite" means by emitting a smaller set.
$canonicalSecurityFixtures = @(
    "BrowserObservedValueChromiumIntegrationTests",
    "BrowserDownloadChromiumIntegrationTests",
    "PersistentBrowserRedirectIntegrationTests",
    "PersistentBrowserSessionIntegrationTests",
    "PlaywrightInFlightCancellationIntegrationTests"
)

function Test-FixtureIdentityInTestName {
    param(
        [Parameter(Mandatory = $true)][string]$TestName,
        [Parameter(Mandatory = $true)][string]$FixtureName
    )

    # VSTest names normally use namespace/class/method dot separators. Match the class as a complete
    # segment rather than a substring so a crafted/unrelated class such as
    # BrowserDownloadChromiumIntegrationTestsFake cannot satisfy release evidence.
    $escapedFixture = [Regex]::Escape($FixtureName)
    return [Regex]::IsMatch($TestName, "(^|\.)$escapedFixture(\.|$)", [Text.RegularExpressions.RegexOptions]::CultureInvariant)
}

$receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
if ([int]$receipt.schemaVersion -ne 2) { throw "Unsupported qualification receipt schemaVersion '$($receipt.schemaVersion)'. Re-run qualification to produce schema v2 digest-bound evidence." }
if ([string]::IsNullOrWhiteSpace([string]$receipt.sourceCommit)) { throw "Qualification receipt has no sourceCommit. Release/judge evidence must come from a Git checkout." }
if ($RequireCleanSource -and $receipt.sourceDirty -ne $false) { throw "Qualification receipt is not from a proven-clean checkout (sourceDirty=$($receipt.sourceDirty))." }
if (-not [string]::IsNullOrWhiteSpace($ExpectedCommit) -and -not [string]::Equals([string]$receipt.sourceCommit, $ExpectedCommit.Trim(), [StringComparison]::OrdinalIgnoreCase)) { throw "Qualification source commit '$($receipt.sourceCommit)' does not match expected commit '$ExpectedCommit'." }
if ($RequireSecuritySuite -and $receipt.securitySuite -ne $true) { throw "Qualification receipt is not from the curated Chromium security suite. Re-run with -SecuritySuite." }

# Verify byte-level evidence integrity before parsing the TRX. This prevents a receipt from being
# paired with a modified/replaced TRX that happens to preserve the same semantic test names.
$declaredTrxSha256 = ([string]$receipt.trxSha256).Trim().ToLowerInvariant()
if ($declaredTrxSha256 -notmatch '^[0-9a-f]{64}$') { throw "Qualification receipt trxSha256 is missing or malformed." }
$actualTrxSha256 = (Get-FileHash -LiteralPath $trxPath -Algorithm SHA256).Hash.ToLowerInvariant()
if (-not [string]::Equals($declaredTrxSha256, $actualTrxSha256, [StringComparison]::Ordinal)) { throw "TRX SHA-256 does not match qualification receipt. Evidence may be stale, mixed, or modified." }

[xml]$trx = Get-Content -LiteralPath $trxPath -Raw
$results = @($trx.TestRun.Results.UnitTestResult)
if ($results.Count -eq 0) { throw "TRX contains zero test results." }
$nonPassed = @($results | Where-Object { [string]$_.outcome -ne "Passed" })
if ($nonPassed.Count -gt 0) {
    $summary = @($nonPassed | ForEach-Object { "'$([string]$_.testName)'=$([string]$_.outcome)" }) -join "; "
    throw "TRX contains non-passed results: $summary"
}
$trxPassedNames = @($results | ForEach-Object { [string]$_.testName } | Sort-Object)
$receiptPassedNames = @($receipt.passedTests | ForEach-Object { [string]$_ } | Sort-Object)
if ([int]$receipt.passedCount -ne $trxPassedNames.Count) { throw "Receipt passedCount '$($receipt.passedCount)' does not match TRX passed count '$($trxPassedNames.Count)'." }
if ($receiptPassedNames.Count -ne $trxPassedNames.Count -or (Compare-Object -ReferenceObject $trxPassedNames -DifferenceObject $receiptPassedNames).Count -ne 0) { throw "Receipt passedTests do not exactly match the TRX PASS evidence." }

$requiredFixtures = @($receipt.requiredFixtures | ForEach-Object { [string]$_ })
if ($receipt.securitySuite -eq $true) {
    if ($requiredFixtures.Count -eq 0) { throw "Security-suite receipt declares no required fixtures." }
    if (@($requiredFixtures | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -gt 0) { throw "Security-suite receipt contains an empty required fixture name." }
    $duplicateFixtures = @($requiredFixtures | Group-Object | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name })
    if ($duplicateFixtures.Count -gt 0) { throw "Security-suite receipt contains duplicate required fixture(s): $($duplicateFixtures -join ', ')." }

    # Exact set equality is critical: securitySuite=true is not sufficient if a buggy or modified
    # producer can omit a canonical boundary and then describe its reduced set as complete.
    $fixtureDifference = @(Compare-Object -ReferenceObject @($canonicalSecurityFixtures | Sort-Object) -DifferenceObject @($requiredFixtures | Sort-Object))
    if ($fixtureDifference.Count -gt 0) {
        throw "Security-suite receipt does not declare the verifier's canonical fixture set. Evidence may come from a reduced or incompatible suite."
    }
    foreach ($requiredFixture in $canonicalSecurityFixtures) {
        $fixturePasses = @($trxPassedNames | Where-Object { Test-FixtureIdentityInTestName -TestName $_ -FixtureName $requiredFixture })
        if ($fixturePasses.Count -eq 0) { throw "Required canonical security fixture '$requiredFixture' has no PASS evidence in the TRX." }
    }
}
elseif ($requiredFixtures.Count -gt 0) {
    throw "Non-security qualification receipt unexpectedly declares required security fixtures. Refusing inconsistent evidence metadata."
}

$allowedReceiptFields = @(
    "schemaVersion", "validatedAtUtc", "sourceCommit", "sourceDirty", "dotnetSdk",
    "configuration", "securitySuite", "effectiveFilter", "trxSha256", "passedCount",
    "requiredFixtures", "passedTests"
)
$unexpectedFields = @($receipt.PSObject.Properties.Name | Where-Object { $_ -notin $allowedReceiptFields })
if ($unexpectedFields.Count -gt 0) { throw "Qualification receipt contains unexpected field(s): $($unexpectedFields -join ', '). Refusing unreviewed evidence schema expansion." }
$validatedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$receipt.validatedAtUtc, [ref]$validatedAt)) { throw "Qualification receipt validatedAtUtc is invalid." }

Write-Host "Chromium qualification evidence verified."
Write-Host "  Commit: $($receipt.sourceCommit)"
Write-Host "  Clean source: $(-not [bool]$receipt.sourceDirty)"
Write-Host "  Passed tests: $($trxPassedNames.Count)"
Write-Host "  Security suite: $([bool]$receipt.securitySuite)"
Write-Host "  TRX SHA-256: $actualTrxSha256"
Write-Host "  Validated UTC: $($validatedAt.ToUniversalTime().ToString('O'))"
