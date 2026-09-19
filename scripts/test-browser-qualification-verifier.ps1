[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$verifier = Join-Path $PSScriptRoot "verify-browser-qualification.ps1"
if (-not (Test-Path -LiteralPath $verifier -PathType Leaf)) { throw "Verifier not found: $verifier" }

$canonicalFixtures = @(
    "BrowserObservedValueChromiumIntegrationTests",
    "BrowserDownloadChromiumIntegrationTests",
    "PersistentBrowserRedirectIntegrationTests",
    "PersistentBrowserSessionIntegrationTests",
    "PlaywrightInFlightCancellationIntegrationTests"
)
$commit = "0123456789abcdef0123456789abcdef01234567"
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("nvidea-verifier-regression-" + [Guid]::NewGuid().ToString("N"))

function Write-Trx {
    param([string]$Directory, [string[]]$Names)
    $results = foreach ($name in $Names) {
        $escaped = [Security.SecurityElement]::Escape($name)
        "    <UnitTestResult testName=\"$escaped\" outcome=\"Passed\" />"
    }
    $xml = @("<?xml version=\"1.0\" encoding=\"utf-8\"?>", "<TestRun>", "  <Results>") + $results + @("  </Results>", "</TestRun>")
    Set-Content -LiteralPath (Join-Path $Directory "browser-integration.trx") -Value ($xml -join [Environment]::NewLine) -Encoding utf8
}

function Write-Receipt {
    param(
        [string]$Directory,
        [string[]]$PassedTests,
        [object]$SourceCommit = $commit,
        [object]$SecuritySuite = $true,
        [object]$RequiredFixtures = $canonicalFixtures
    )
    $trxPath = Join-Path $Directory "browser-integration.trx"
    $receipt = [ordered]@{
        schemaVersion = 2
        validatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        sourceCommit = $SourceCommit
        sourceDirty = $false
        dotnetSdk = "8.0.100"
        configuration = "Release"
        securitySuite = $SecuritySuite
        effectiveFilter = "hermetic-regression-fixture"
        trxSha256 = (Get-FileHash -LiteralPath $trxPath -Algorithm SHA256).Hash.ToLowerInvariant()
        passedCount = $PassedTests.Count
        requiredFixtures = $RequiredFixtures
        passedTests = $PassedTests
    }
    $receipt | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Directory "qualification-receipt.json") -Encoding utf8
}

function New-Case {
    param([string]$Name)
    $directory = Join-Path $tempRoot $Name
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $names = @($canonicalFixtures | ForEach-Object { "Nvidea.Core.Tests.$_.Passes" })
    Write-Trx -Directory $directory -Names $names
    Write-Receipt -Directory $directory -PassedTests $names
    return $directory
}

function Invoke-Case {
    param([string]$Name, [string]$Directory, [bool]$ShouldPass)
    $output = & $verifier -EvidenceDirectory $Directory -RequireCleanSource -RequireSecuritySuite -ExpectedCommit $commit 2>&1
    $passed = $LASTEXITCODE -eq 0
    if ($passed -ne $ShouldPass) {
        throw "Regression case '$Name' expected pass=$ShouldPass but observed pass=$passed. Output: $($output -join [Environment]::NewLine)"
    }
    Write-Host "PASS regression case: $Name (accepted=$passed)"
}

New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
try {
    $baseline = New-Case -Name "baseline"
    Invoke-Case -Name "valid canonical evidence" -Directory $baseline -ShouldPass $true

    $badCommit = New-Case -Name "bad-commit"
    Write-Receipt -Directory $badCommit -PassedTests @($canonicalFixtures | ForEach-Object { "Nvidea.Core.Tests.$_.Passes" }) -SourceCommit "main"
    Invoke-Case -Name "symbolic source commit rejected" -Directory $badCommit -ShouldPass $false

    $typeConfusion = New-Case -Name "type-confusion"
    Write-Receipt -Directory $typeConfusion -PassedTests @($canonicalFixtures | ForEach-Object { "Nvidea.Core.Tests.$_.Passes" }) -SecuritySuite "true"
    Invoke-Case -Name "string boolean rejected" -Directory $typeConfusion -ShouldPass $false

    $reduced = New-Case -Name "reduced-suite"
    $reducedFixtures = @($canonicalFixtures | Select-Object -First 4)
    $reducedNames = @($reducedFixtures | ForEach-Object { "Nvidea.Core.Tests.$_.Passes" })
    Write-Trx -Directory $reduced -Names $reducedNames
    Write-Receipt -Directory $reduced -PassedTests $reducedNames -RequiredFixtures $reducedFixtures
    Invoke-Case -Name "reduced security suite rejected" -Directory $reduced -ShouldPass $false

    $lookalike = New-Case -Name "lookalike-fixture"
    $lookalikeNames = @($canonicalFixtures | ForEach-Object { "Nvidea.Core.Tests.$_.Passes" })
    $lookalikeNames[0] = "Nvidea.Core.Tests.$($canonicalFixtures[0])Fake.Passes"
    Write-Trx -Directory $lookalike -Names $lookalikeNames
    Write-Receipt -Directory $lookalike -PassedTests $lookalikeNames
    Invoke-Case -Name "fixture lookalike rejected" -Directory $lookalike -ShouldPass $false

    $tampered = New-Case -Name "tampered-trx"
    Add-Content -LiteralPath (Join-Path $tampered "browser-integration.trx") -Value "<!-- tampered after receipt -->"
    Invoke-Case -Name "TRX digest tampering rejected" -Directory $tampered -ShouldPass $false

    Write-Host "All qualification verifier regression cases passed."
}
finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue }
}
