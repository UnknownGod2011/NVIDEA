[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$preflight = Join-Path $repoRoot "scripts/submission-preflight.ps1"
if (-not (Test-Path -LiteralPath $preflight -PathType Leaf)) { throw "Preflight script missing: $preflight" }
if (-not (Test-Path -LiteralPath (Join-Path $repoRoot "docs/demo-package.json") -PathType Leaf)) { throw "Demo manifest missing." }

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Read-Invocations([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return @() }
    return @(Get-Content -LiteralPath $Path | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

$temp = Join-Path ([IO.Path]::GetTempPath()) ("nvidea-preflight-behavior-" + [Guid]::NewGuid().ToString("N"))
$shimDir = Join-Path $temp "bin"
$log = Join-Path $temp "dotnet-invocations.log"
New-Item -ItemType Directory -Path $shimDir -Force | Out-Null

# Windows recording machines resolve dotnet.cmd from PATH before the real SDK. This provider-free
# shim records the child command, can inject a non-zero exit, and normally materializes the --output
# file. NVIDEA_FAKE_DOTNET_WITHHOLD_FRAGMENT simulates a child that lies with exit 0 but no artifact.
$shim = Join-Path $shimDir "dotnet.cmd"
@'
@echo off
setlocal EnableExtensions
>>"%NVIDEA_FAKE_DOTNET_LOG%" echo %*
echo %* | findstr /C:"%NVIDEA_FAKE_DOTNET_FAIL_FRAGMENT%" >nul
if not "%NVIDEA_FAKE_DOTNET_FAIL_FRAGMENT%"=="" if not errorlevel 1 exit /b 41
echo %* | findstr /C:"%NVIDEA_FAKE_DOTNET_WITHHOLD_FRAGMENT%" >nul
if not "%NVIDEA_FAKE_DOTNET_WITHHOLD_FRAGMENT%"=="" if not errorlevel 1 exit /b 0
:scan
if "%~1"=="" goto done
if /I "%~1"=="--output" goto output
shift
goto scan
:output
shift
if "%~1"=="" exit /b 42
>"%~1" echo {"fake":true}
:done
exit /b 0
'@ | Set-Content -LiteralPath $shim -Encoding Ascii

$oldPath = $env:PATH
$oldLog = $env:NVIDEA_FAKE_DOTNET_LOG
$oldFail = $env:NVIDEA_FAKE_DOTNET_FAIL_FRAGMENT
$oldWithhold = $env:NVIDEA_FAKE_DOTNET_WITHHOLD_FRAGMENT
try {
    $env:PATH = $shimDir + [IO.Path]::PathSeparator + $oldPath
    $env:NVIDEA_FAKE_DOTNET_LOG = $log
    $env:NVIDEA_FAKE_DOTNET_WITHHOLD_FRAGMENT = "__never_match__"

    # Fault injection: validator failure must prevent every downstream child invocation.
    $env:NVIDEA_FAKE_DOTNET_FAIL_FRAGMENT = "Nvidea.DemoPackageValidator.csproj"
    Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
    $failedClosed = $false
    try {
        & $preflight -RepositoryRoot $repoRoot -ArtifactsDirectory "artifacts/preflight-behavior-failure"
    }
    catch {
        $failedClosed = $true
        Assert-True ($_.Exception.Message.Contains("Validate schema-v2 demo package failed with exit code 41")) "Unexpected validator-failure error: $($_.Exception.Message)"
    }
    Assert-True $failedClosed "Validator fault injection must fail the preflight."
    $calls = Read-Invocations $log
    Assert-True ($calls.Count -eq 1) "Validator failure must stop downstream execution; observed $($calls.Count) dotnet calls."
    Assert-True ($calls[0].Contains("Nvidea.DemoPackageValidator.csproj")) "The sole failed invocation must be the validator."

    # False-success injection: zero exit without the validator artifact must also stop downstream work.
    $env:NVIDEA_FAKE_DOTNET_FAIL_FRAGMENT = "__never_match__"
    $env:NVIDEA_FAKE_DOTNET_WITHHOLD_FRAGMENT = "Nvidea.DemoPackageValidator.csproj"
    Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
    $withheldClosed = $false
    try {
        & $preflight -RepositoryRoot $repoRoot -ArtifactsDirectory "artifacts/preflight-behavior-withheld"
    }
    catch {
        $withheldClosed = $true
        Assert-True ($_.Exception.Message.Contains("returned success but did not materialize expected output")) "Unexpected missing-output error: $($_.Exception.Message)"
    }
    Assert-True $withheldClosed "Zero exit without expected output must fail the preflight."
    $calls = Read-Invocations $log
    Assert-True ($calls.Count -eq 1) "Missing validator output must stop downstream execution; observed $($calls.Count) dotnet calls."

    # Success sequencing: exactly the four zero-cost projects execute and each shim child creates output.
    $env:NVIDEA_FAKE_DOTNET_WITHHOLD_FRAGMENT = "__never_match__"
    Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
    & $preflight -RepositoryRoot $repoRoot -ArtifactsDirectory "artifacts/preflight-behavior-success"
    $calls = Read-Invocations $log
    Assert-True ($calls.Count -eq 4) "Successful preflight must execute exactly four zero-cost dotnet children."
    $expected = @(
        "Nvidea.DemoPackageValidator.csproj",
        "Nvidea.DemoChecklistGenerator.csproj",
        "Nvidea.PersonalAiDemoEval.csproj",
        "Nvidea.PersonalAiAdversarialEval.csproj"
    )
    for ($i = 0; $i -lt $expected.Count; $i++) {
        Assert-True ($calls[$i].Contains($expected[$i])) "Invocation $i did not match expected project $($expected[$i])."
    }
    foreach ($artifact in @("demo-package-validation.json", "recording-checklist.md", "personal-ai-positive.json", "personal-ai-adversarial.json")) {
        $path = Join-Path $repoRoot "artifacts/preflight-behavior-success/$artifact"
        Assert-True ((Test-Path -LiteralPath $path -PathType Leaf) -and ((Get-Item -LiteralPath $path).Length -gt 0)) "Expected fresh non-empty artifact missing: $artifact"
    }
    $joined = $calls -join "`n"
    foreach ($forbidden in @("Nvidea.JudgingEvidenceVerifier", "NebiusLive", "TavilyLive", "PlaywrightLive")) {
        Assert-True (-not $joined.Contains($forbidden)) "Zero-cost behavior reached forbidden provider-live command fragment: $forbidden"
    }

    # Repository confinement must reject an escape before any dotnet child is launched.
    Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
    $escaped = Join-Path $temp "escaped-artifacts"
    $escapeRejected = $false
    try {
        & $preflight -RepositoryRoot $repoRoot -ArtifactsDirectory $escaped
    }
    catch {
        $escapeRejected = $true
        Assert-True ($_.Exception.Message.Contains("ArtifactsDirectory must remain inside the repository.")) "Unexpected path-escape error: $($_.Exception.Message)"
    }
    Assert-True $escapeRejected "Artifact path escape must be rejected."
    Assert-True ((Read-Invocations $log).Count -eq 0) "Artifact path rejection must occur before any dotnet child executes."

    Write-Host "submission-preflight behavior PASS (fake-dotnet fault injection; network/provider free)."
}
finally {
    $env:PATH = $oldPath
    $env:NVIDEA_FAKE_DOTNET_LOG = $oldLog
    $env:NVIDEA_FAKE_DOTNET_FAIL_FRAGMENT = $oldFail
    $env:NVIDEA_FAKE_DOTNET_WITHHOLD_FRAGMENT = $oldWithhold
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
    foreach ($name in @("failure", "withheld", "success")) {
        Remove-Item -LiteralPath (Join-Path $repoRoot "artifacts/preflight-behavior-$name") -Recurse -Force -ErrorAction SilentlyContinue
    }
}
