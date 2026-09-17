[CmdletBinding()]
param(
    [switch]$ContinueOnFailure,
    [string]$EvidencePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$testsRoot = Join-Path $PSScriptRoot 'tests'

$tests = @(
    'submission-preflight.contract.ps1',
    'submission-preflight.behavior.ps1',
    'submission-preflight.duplicate-json.ps1',
    'submission-preflight.schema-shape.ps1',
    'preflight-receipt-tamper.contract.ps1',
    'preflight-receipt-tamper.behavior.ps1'
)

if ($PSVersionTable.PSVersion.Major -lt 7) {
    throw "Submission tooling tests require PowerShell 7 or newer. Current version: $($PSVersionTable.PSVersion)."
}

function Resolve-ConfinedEvidencePath {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { return $null }

    $candidate = if ([System.IO.Path]::IsPathRooted($Path)) {
        [System.IO.Path]::GetFullPath($Path)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $Path))
    }

    if (-not $candidate.StartsWith($repositoryRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "EvidencePath must remain beneath the repository root: $candidate"
    }
    if ([System.IO.Path]::GetExtension($candidate) -ne '.json') {
        throw "EvidencePath must use a .json extension: $candidate"
    }
    return $candidate
}

$resolvedEvidencePath = Resolve-ConfinedEvidencePath -Path $EvidencePath
$missing = @($tests | Where-Object { -not (Test-Path -LiteralPath (Join-Path $testsRoot $_) -PathType Leaf) })
if ($missing.Count -gt 0) {
    throw "Missing submission tooling tests: $($missing -join ', ')"
}

$results = [System.Collections.Generic.List[object]]::new()
$startedAt = [DateTimeOffset]::UtcNow

foreach ($test in $tests) {
    $testPath = [System.IO.Path]::GetFullPath((Join-Path $testsRoot $test))
    if (-not $testPath.StartsWith($repositoryRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to execute test outside repository root: $testPath"
    }

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $output = @()
    $passed = $false
    $failure = $null

    try {
        $output = @(& $testPath 2>&1)
        $passed = $true
    }
    catch {
        $failure = $_.Exception.Message
        $output += $_
    }
    finally {
        $stopwatch.Stop()
    }

    $results.Add([pscustomobject]@{
        test = $test
        passed = $passed
        durationMs = $stopwatch.ElapsedMilliseconds
        failure = $failure
    })

    if ($passed) {
        Write-Host "PASS $test ($($stopwatch.ElapsedMilliseconds) ms)"
    }
    else {
        Write-Host "FAIL $test ($($stopwatch.ElapsedMilliseconds) ms): $failure"
        foreach ($line in $output) { Write-Host "  $line" }
        if (-not $ContinueOnFailure) { break }
    }
}

$completedAt = [DateTimeOffset]::UtcNow
$failed = @($results | Where-Object { -not $_.passed })
$executed = $results.Count
$allExpectedExecuted = $executed -eq $tests.Count
$overallPassed = ($failed.Count -eq 0) -and $allExpectedExecuted

if ($resolvedEvidencePath) {
    $evidenceDirectory = Split-Path -Parent $resolvedEvidencePath
    [System.IO.Directory]::CreateDirectory($evidenceDirectory) | Out-Null
    $relativeEvidencePath = [System.IO.Path]::GetRelativePath($repositoryRoot, $resolvedEvidencePath).Replace('\\', '/')
    $evidence = [ordered]@{
        schemaVersion = 1
        scope = 'local-zero-cost'
        providerLiveEvidence = $false
        startedAtUtc = $startedAt.ToString('O')
        completedAtUtc = $completedAt.ToString('O')
        powerShellVersion = $PSVersionTable.PSVersion.ToString()
        os = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
        architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
        expectedTestCount = $tests.Count
        executedTestCount = $executed
        allExpectedExecuted = $allExpectedExecuted
        overallPassed = $overallPassed
        continueOnFailure = [bool]$ContinueOnFailure
        evidencePath = $relativeEvidencePath
        results = @($results)
    }
    $json = $evidence | ConvertTo-Json -Depth 6
    $tempPath = "$resolvedEvidencePath.tmp-$([Guid]::NewGuid().ToString('N'))"
    try {
        [System.IO.File]::WriteAllText($tempPath, $json, [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::Move($tempPath, $resolvedEvidencePath, $true)
    }
    finally {
        if (Test-Path -LiteralPath $tempPath) { Remove-Item -LiteralPath $tempPath -Force }
    }
    Write-Host "Evidence: $resolvedEvidencePath"
}

Write-Host ""
Write-Host "Submission tooling regression summary: $($executed - $failed.Count)/$executed passed."
Write-Host "Started UTC: $($startedAt.ToString('O'))"
Write-Host "Completed UTC: $($completedAt.ToString('O'))"

if ($failed.Count -gt 0) {
    throw "$($failed.Count) submission tooling regression(s) failed."
}

if (-not $allExpectedExecuted) {
    throw "Submission tooling suite did not execute all expected tests ($executed/$($tests.Count))."
}

Write-Host 'All zero-cost submission tooling regressions PASS.'
