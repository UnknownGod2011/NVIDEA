[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$preflight = Join-Path $repoRoot 'scripts/submission-preflight.ps1'
$manifestPath = Join-Path $repoRoot 'docs/demo-package.json'
if (-not (Test-Path -LiteralPath $preflight -PathType Leaf)) { throw 'Preflight script missing.' }
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw 'Demo manifest missing.' }

function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Read-Calls([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return @() }
    @(Get-Content -LiteralPath $Path | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}
function Assert-RejectedBefore {
    param([string]$ArtifactsName,[string]$Fixture,[int]$ExpectedCalls,[string]$ExpectedMessage)
    $env:NVIDEA_SCHEMA_VALIDATOR_FIXTURE = $Fixture
    Remove-Item $script:log -Force -ErrorAction SilentlyContinue
    $closed = $false
    try { & $script:preflight -RepositoryRoot $script:repoRoot -ArtifactsDirectory "artifacts/$ArtifactsName" }
    catch {
        $closed = $true
        Assert-True $_.Exception.Message.Contains($ExpectedMessage) "Unexpected failure for $ArtifactsName`: $($_.Exception.Message)"
    }
    Assert-True $closed "$ArtifactsName must fail closed."
    Assert-True ((Read-Calls $script:log).Count -eq $ExpectedCalls) "$ArtifactsName executed an unexpected number of children."
}

$temp = Join-Path ([IO.Path]::GetTempPath()) ('nvidea-schema-shape-' + [Guid]::NewGuid().ToString('N'))
$shimDir = Join-Path $temp 'bin'
$log = Join-Path $temp 'calls.log'
New-Item -ItemType Directory -Path $shimDir -Force | Out-Null
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$duration = (@($manifest.beats) | Measure-Object -Property durationSeconds -Sum).Sum
$manifestSha = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToLowerInvariant()

$validValidator = @{schemaVersion=2;overallPassed=$true;manifestSha256=$manifestSha;declaredMaximumDurationSeconds=[int]$manifest.maxDurationSeconds;plannedDurationSeconds=[int]$duration;beatCount=@($manifest.beats).Count;checks=@(@{id='fixture-pass';passed=$true;requirement='schema-shape harness'})}
$validatorTop = Join-Path $temp 'validator-top.json'
($validValidator + @{unexpectedTrustSignal='smuggled'}) | ConvertTo-Json -Depth 8 -Compress | Set-Content -LiteralPath $validatorTop -Encoding UTF8
$validatorNested = Join-Path $temp 'validator-nested.json'
$nested = $validValidator.Clone(); $nested.checks = @(@{id='fixture-pass';passed=$true;requirement='schema-shape harness';unexpectedTrustSignal='smuggled'})
$nested | ConvertTo-Json -Depth 8 -Compress | Set-Content -LiteralPath $validatorNested -Encoding UTF8
$validValidatorPath = Join-Path $temp 'validator-valid.json'
$validValidator | ConvertTo-Json -Depth 8 -Compress | Set-Content -LiteralPath $validValidatorPath -Encoding UTF8

$evalTop = Join-Path $temp 'eval-top.json'
@{schemaVersion=1;generatedAt='2026-09-17T00:00:00Z';overallPassed=$true;checks=@(@{id='fixture-pass';passed=$true;detail='schema-shape harness'});unexpectedTrustSignal='smuggled'} | ConvertTo-Json -Depth 8 -Compress | Set-Content -LiteralPath $evalTop -Encoding UTF8
$evalNested = Join-Path $temp 'eval-nested.json'
@{schemaVersion=1;generatedAt='2026-09-17T00:00:00Z';overallPassed=$true;checks=@(@{id='fixture-pass';passed=$true;detail='schema-shape harness';unexpectedTrustSignal='smuggled'})} | ConvertTo-Json -Depth 8 -Compress | Set-Content -LiteralPath $evalNested -Encoding UTF8

$checklist = Join-Path $temp 'checklist.md'
$lines = @('# Generated Judge Demo Checklist','')
for ($i=0; $i -lt @($manifest.beats).Count; $i++) {
    $beat = @($manifest.beats)[$i]
    $lines += "## $($i+1). $($beat.label) ($($beat.durationSeconds)s)"
    foreach ($milestone in @($beat.expectedSessionMilestones)) { $lines += "- [ ] ``$milestone`` is visible only after genuine production observation." }
}
$lines += '## Take acceptance gate'
Set-Content -LiteralPath $checklist -Value $lines -Encoding UTF8

$shim = Join-Path $shimDir 'dotnet.cmd'
@'
@echo off
setlocal EnableExtensions
>>"%NVIDEA_SCHEMA_LOG%" echo %*
set "project=%*"
:scan
if "%~1"=="" goto done
if /I "%~1"=="--output" goto output
shift
goto scan
:output
shift
if "%~1"=="" exit /b 42
echo %project% | findstr /C:"Nvidea.DemoPackageValidator.csproj" >nul && copy /Y "%NVIDEA_SCHEMA_VALIDATOR_FIXTURE%" "%~1" >nul && exit /b 0
echo %project% | findstr /C:"Nvidea.DemoChecklistGenerator.csproj" >nul && copy /Y "%NVIDEA_SCHEMA_CHECKLIST_FIXTURE%" "%~1" >nul && exit /b 0
copy /Y "%NVIDEA_SCHEMA_EVAL_FIXTURE%" "%~1" >nul
:done
exit /b 0
'@ | Set-Content -LiteralPath $shim -Encoding Ascii

$names = @('PATH','NVIDEA_SCHEMA_LOG','NVIDEA_SCHEMA_VALIDATOR_FIXTURE','NVIDEA_SCHEMA_CHECKLIST_FIXTURE','NVIDEA_SCHEMA_EVAL_FIXTURE')
$old = @{}; foreach ($name in $names) { $old[$name] = [Environment]::GetEnvironmentVariable($name) }
try {
    $env:PATH = $shimDir + [IO.Path]::PathSeparator + $old.PATH
    $env:NVIDEA_SCHEMA_LOG = $log
    $env:NVIDEA_SCHEMA_CHECKLIST_FIXTURE = $checklist

    $env:NVIDEA_SCHEMA_EVAL_FIXTURE = $evalTop
    Assert-RejectedBefore 'preflight-schema-validator-top' $validatorTop 1 "unexpected JSON property 'unexpectedTrustSignal' at $."
    Assert-RejectedBefore 'preflight-schema-validator-nested' $validatorNested 1 "unexpected JSON property 'unexpectedTrustSignal' at $.checks[0]"

    $env:NVIDEA_SCHEMA_VALIDATOR_FIXTURE = $validValidatorPath
    Remove-Item $log -Force -ErrorAction SilentlyContinue
    $closed = $false
    try { & $preflight -RepositoryRoot $repoRoot -ArtifactsDirectory 'artifacts/preflight-schema-evaluator-top' }
    catch { $closed=$true; Assert-True $_.Exception.Message.Contains("unexpected JSON property 'unexpectedTrustSignal' at $.") "Unexpected evaluator top-level failure: $($_.Exception.Message)" }
    Assert-True $closed 'Unexpected evaluator top-level property must fail closed.'
    Assert-True ((Read-Calls $log).Count -eq 3) 'Unexpected positive-evaluator top-level property reached the adversarial evaluator.'

    $env:NVIDEA_SCHEMA_EVAL_FIXTURE = $evalNested
    Remove-Item $log -Force -ErrorAction SilentlyContinue
    $closed = $false
    try { & $preflight -RepositoryRoot $repoRoot -ArtifactsDirectory 'artifacts/preflight-schema-evaluator-nested' }
    catch { $closed=$true; Assert-True $_.Exception.Message.Contains("unexpected JSON property 'unexpectedTrustSignal' at $.checks[0]") "Unexpected evaluator nested failure: $($_.Exception.Message)" }
    Assert-True $closed 'Unexpected evaluator nested check property must fail closed.'
    Assert-True ((Read-Calls $log).Count -eq 3) 'Unexpected positive-evaluator nested property reached the adversarial evaluator.'

    Write-Host 'submission-preflight schema-shape PASS (unexpected-field fault injection; network/provider free).'
}
finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name,$old[$name]) }
    Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue
    foreach ($name in @('preflight-schema-validator-top','preflight-schema-validator-nested','preflight-schema-evaluator-top','preflight-schema-evaluator-nested')) { Remove-Item (Join-Path $repoRoot "artifacts/$name") -Recurse -Force -ErrorAction SilentlyContinue }
}
