[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$preflight = Join-Path $repoRoot "scripts/submission-preflight.ps1"
$manifestPath = Join-Path $repoRoot "docs/demo-package.json"
if (-not (Test-Path -LiteralPath $preflight -PathType Leaf)) { throw "Preflight script missing." }
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw "Demo manifest missing." }

function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Read-Invocations([string]$Path) { if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return @() }; return @(Get-Content -LiteralPath $Path | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) }

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$duration = (@($manifest.beats) | Measure-Object -Property durationSeconds -Sum).Sum
$manifestSha = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
$temp = Join-Path ([IO.Path]::GetTempPath()) ("nvidea-preflight-duplicate-json-" + [Guid]::NewGuid().ToString("N"))
$shimDir = Join-Path $temp "bin"
$log = Join-Path $temp "dotnet.log"
New-Item -ItemType Directory -Path $shimDir -Force | Out-Null

$basePrefix = '{"schemaVersion":2,"overallPassed":true,"manifestSha256":"' + $manifestSha + '","plannedDurationSeconds":' + [string]$duration + ',"beatCount":' + [string]@($manifest.beats).Count + ','
$fixtures = @{
    "schemaVersion" = '{"schemaVersion":2,"schemaVersion":1,"overallPassed":true,"manifestSha256":"' + $manifestSha + '","plannedDurationSeconds":' + [string]$duration + ',"beatCount":' + [string]@($manifest.beats).Count + ',"checks":[{"id":"pass","passed":true}]}'
    "overallPassed" = '{"schemaVersion":2,"overallPassed":true,"overallPassed":false,"manifestSha256":"' + $manifestSha + '","plannedDurationSeconds":' + [string]$duration + ',"beatCount":' + [string]@($manifest.beats).Count + ',"checks":[{"id":"pass","passed":true}]}'
    "manifestSha256" = '{"schemaVersion":2,"overallPassed":true,"manifestSha256":"' + $manifestSha + '","manifestSha256":"' + ('0' * 64) + '","plannedDurationSeconds":' + [string]$duration + ',"beatCount":' + [string]@($manifest.beats).Count + ',"checks":[{"id":"pass","passed":true}]}'
    "nested-check-passed" = $basePrefix + '"checks":[{"id":"pass","passed":true,"passed":false}]}'
}
$fixturePaths = @{}
foreach ($name in $fixtures.Keys) {
    $path = Join-Path $temp ("$name.json")
    [IO.File]::WriteAllText($path, $fixtures[$name], [Text.UTF8Encoding]::new($false))
    $fixturePaths[$name] = $path
}

$shim = Join-Path $shimDir "dotnet.cmd"
@'
@echo off
setlocal EnableExtensions
>>"%NVIDEA_DUP_JSON_LOG%" echo %*
:scan
if "%~1"=="" exit /b 43
if /I "%~1"=="--output" goto output
shift
goto scan
:output
shift
if "%~1"=="" exit /b 44
copy /Y "%NVIDEA_DUP_JSON_FIXTURE%" "%~1" >nul
exit /b 0
'@ | Set-Content -LiteralPath $shim -Encoding Ascii

$names = @("PATH", "NVIDEA_DUP_JSON_LOG", "NVIDEA_DUP_JSON_FIXTURE")
$old = @{}
foreach ($name in $names) { $old[$name] = [Environment]::GetEnvironmentVariable($name) }
try {
    $env:PATH = $shimDir + [IO.Path]::PathSeparator + $old["PATH"]
    $env:NVIDEA_DUP_JSON_LOG = $log
    foreach ($caseName in @("schemaVersion", "overallPassed", "manifestSha256", "nested-check-passed")) {
        $env:NVIDEA_DUP_JSON_FIXTURE = $fixturePaths[$caseName]
        Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
        $artifactDir = "artifacts/preflight-duplicate-json-$caseName"
        $closed = $false
        try { & $preflight -RepositoryRoot $repoRoot -ArtifactsDirectory $artifactDir }
        catch {
            $closed = $true
            Assert-True $_.Exception.Message.Contains("duplicate JSON property") "Case '$caseName' failed for an unexpected reason: $($_.Exception.Message)"
        }
        Assert-True $closed "Duplicate JSON case '$caseName' was accepted."
        Assert-True ((Read-Invocations $log).Count -eq 1) "Duplicate JSON case '$caseName' leaked into downstream execution."
        Remove-Item -LiteralPath (Join-Path $repoRoot $artifactDir) -Recurse -Force -ErrorAction SilentlyContinue
    }
    Write-Host "submission-preflight duplicate JSON PASS (four ambiguity fixtures rejected before downstream execution)."
}
finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $old[$name]) }
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
    foreach ($caseName in @("schemaVersion", "overallPassed", "manifestSha256", "nested-check-passed")) { Remove-Item -LiteralPath (Join-Path $repoRoot "artifacts/preflight-duplicate-json-$caseName") -Recurse -Force -ErrorAction SilentlyContinue }
}
