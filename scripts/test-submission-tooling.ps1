[CmdletBinding()]
param(
    [switch]$ContinueOnFailure
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
        Test = $test
        Passed = $passed
        DurationMs = $stopwatch.ElapsedMilliseconds
        Failure = $failure
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
$failed = @($results | Where-Object { -not $_.Passed })
$executed = $results.Count

Write-Host ""
Write-Host "Submission tooling regression summary: $($executed - $failed.Count)/$executed passed."
Write-Host "Started UTC: $($startedAt.ToString('O'))"
Write-Host "Completed UTC: $($completedAt.ToString('O'))"

if ($failed.Count -gt 0) {
    throw "$($failed.Count) submission tooling regression(s) failed."
}

if ($executed -ne $tests.Count) {
    throw "Submission tooling suite did not execute all expected tests ($executed/$($tests.Count))."
}

Write-Host 'All zero-cost submission tooling regressions PASS.'
