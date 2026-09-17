[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference="Stop"
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."));$preflight=Join-Path $repoRoot "scripts/submission-preflight.ps1";$manifestPath=Join-Path $repoRoot "docs/demo-package.json"
if(-not(Test-Path -LiteralPath $preflight -PathType Leaf)){throw "Preflight script missing"};if(-not(Test-Path -LiteralPath $manifestPath -PathType Leaf)){throw "Demo manifest missing"}
function Assert-True([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Read-Invocations([string]$Path){if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){return @()};return @(Get-Content -LiteralPath $Path|Where-Object{-not[string]::IsNullOrWhiteSpace($_)})}
$temp=Join-Path ([IO.Path]::GetTempPath()) ("nvidea-preflight-behavior-"+[Guid]::NewGuid().ToString("N"));$shimDir=Join-Path $temp "bin";$log=Join-Path $temp "dotnet-invocations.log";New-Item -ItemType Directory -Path $shimDir -Force|Out-Null
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json;$duration=(@($manifest.beats)|Measure-Object -Property durationSeconds -Sum).Sum;$manifestSha=(Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
$validatorFixture=Join-Path $temp "validator.json";@{schemaVersion=2;overallPassed=$true;manifestSha256=$manifestSha;declaredMaximumDurationSeconds=[int]$manifest.maxDurationSeconds;plannedDurationSeconds=[int]$duration;beatCount=@($manifest.beats).Count;checks=@(@{id="fixture-pass";passed=$true;requirement="behavior harness"})}|ConvertTo-Json -Depth 8 -Compress|Set-Content -LiteralPath $validatorFixture -Encoding UTF8
$wrongHashFixture=Join-Path $temp "validator-wrong-hash.json";@{schemaVersion=2;overallPassed=$true;manifestSha256=("0"*64);declaredMaximumDurationSeconds=[int]$manifest.maxDurationSeconds;plannedDurationSeconds=[int]$duration;beatCount=@($manifest.beats).Count;checks=@(@{id="fixture-pass";passed=$true})}|ConvertTo-Json -Depth 8 -Compress|Set-Content -LiteralPath $wrongHashFixture -Encoding UTF8
$evalFixture=Join-Path $temp "eval.json";@{schemaVersion=1;generatedAt="2026-09-17T00:00:00Z";overallPassed=$true;checks=@(@{id="fixture-pass";passed=$true;detail="behavior harness"})}|ConvertTo-Json -Depth 8 -Compress|Set-Content -LiteralPath $evalFixture -Encoding UTF8
$checklistFixture=Join-Path $temp "checklist.md";$lines=@("# Generated Judge Demo Checklist","");for($i=0;$i-lt @($manifest.beats).Count;$i++){$b=@($manifest.beats)[$i];$lines+="## $($i+1). $($b.label) ($($b.durationSeconds)s)";foreach($m in @($b.expectedSessionMilestones)){$lines+="- [ ] ``$m`` is visible only after genuine production observation."}};$lines+="## Take acceptance gate";Set-Content -LiteralPath $checklistFixture -Value $lines -Encoding UTF8
$shim=Join-Path $shimDir "dotnet.cmd"
@'
@echo off
setlocal EnableExtensions
>>"%NVIDEA_FAKE_DOTNET_LOG%" echo %*
echo %* | findstr /C:"%NVIDEA_FAKE_DOTNET_FAIL_FRAGMENT%" >nul
if not "%NVIDEA_FAKE_DOTNET_FAIL_FRAGMENT%"=="" if not errorlevel 1 exit /b 41
echo %* | findstr /C:"%NVIDEA_FAKE_DOTNET_WITHHOLD_FRAGMENT%" >nul
if not "%NVIDEA_FAKE_DOTNET_WITHHOLD_FRAGMENT%"=="" if not errorlevel 1 exit /b 0
set "project=%*"
:scan
if "%~1"=="" goto done
if /I "%~1"=="--output" goto output
shift
goto scan
:output
shift
if "%~1"=="" exit /b 42
if not "%NVIDEA_FAKE_DOTNET_MALFORM_FRAGMENT%"=="" echo %project% | findstr /C:"%NVIDEA_FAKE_DOTNET_MALFORM_FRAGMENT%" >nul && (>"%~1" echo {"schemaVersion":1,"overallPassed":false,"checks":[{"id":"injected","passed":false}]}) && exit /b 0
echo %project% | findstr /C:"Nvidea.DemoPackageValidator.csproj" >nul && copy /Y "%NVIDEA_FAKE_VALIDATOR_FIXTURE%" "%~1" >nul && exit /b 0
echo %project% | findstr /C:"Nvidea.DemoChecklistGenerator.csproj" >nul && copy /Y "%NVIDEA_FAKE_CHECKLIST_FIXTURE%" "%~1" >nul && exit /b 0
copy /Y "%NVIDEA_FAKE_EVAL_FIXTURE%" "%~1" >nul
:done
exit /b 0
'@|Set-Content -LiteralPath $shim -Encoding Ascii
$names=@("PATH","NVIDEA_FAKE_DOTNET_LOG","NVIDEA_FAKE_DOTNET_FAIL_FRAGMENT","NVIDEA_FAKE_DOTNET_WITHHOLD_FRAGMENT","NVIDEA_FAKE_DOTNET_MALFORM_FRAGMENT","NVIDEA_FAKE_VALIDATOR_FIXTURE","NVIDEA_FAKE_CHECKLIST_FIXTURE","NVIDEA_FAKE_EVAL_FIXTURE");$old=@{};foreach($n in $names){$old[$n]=[Environment]::GetEnvironmentVariable($n)}
try{
 $env:PATH=$shimDir+[IO.Path]::PathSeparator+$old["PATH"];$env:NVIDEA_FAKE_DOTNET_LOG=$log;$env:NVIDEA_FAKE_VALIDATOR_FIXTURE=$validatorFixture;$env:NVIDEA_FAKE_CHECKLIST_FIXTURE=$checklistFixture;$env:NVIDEA_FAKE_EVAL_FIXTURE=$evalFixture;$env:NVIDEA_FAKE_DOTNET_WITHHOLD_FRAGMENT="__never_match__";$env:NVIDEA_FAKE_DOTNET_MALFORM_FRAGMENT="__never_match__"
 $env:NVIDEA_FAKE_DOTNET_FAIL_FRAGMENT="Nvidea.DemoPackageValidator.csproj";Remove-Item $log -Force -ErrorAction SilentlyContinue;$closed=$false;try{& $preflight -RepositoryRoot $repoRoot -ArtifactsDirectory "artifacts/preflight-behavior-failure"}catch{$closed=$true;Assert-True $_.Exception.Message.Contains("exit code 41") "Unexpected validator failure"};Assert-True $closed "Validator failure must close";Assert-True ((Read-Invocations $log).Count-eq1) "Validator failure leaked downstream"
 $env:NVIDEA_FAKE_DOTNET_FAIL_FRAGMENT="__never_match__";$env:NVIDEA_FAKE_DOTNET_WITHHOLD_FRAGMENT="Nvidea.DemoPackageValidator.csproj";Remove-Item $log -Force -ErrorAction SilentlyContinue;$closed=$false;try{& $preflight -RepositoryRoot $repoRoot -ArtifactsDirectory "artifacts/preflight-behavior-withheld"}catch{$closed=$true;Assert-True $_.Exception.Message.Contains("did not materialize") "Unexpected missing artifact failure"};Assert-True $closed "Withheld artifact must close";Assert-True ((Read-Invocations $log).Count-eq1) "Withheld validator leaked downstream"
 $env:NVIDEA_FAKE_DOTNET_WITHHOLD_FRAGMENT="__never_match__";$env:NVIDEA_FAKE_VALIDATOR_FIXTURE=$wrongHashFixture;Remove-Item $log -Force -ErrorAction SilentlyContinue;$closed=$false;try{& $preflight -RepositoryRoot $repoRoot -ArtifactsDirectory "artifacts/preflight-behavior-wronghash"}catch{$closed=$true;Assert-True $_.Exception.Message.Contains("manifest SHA-256") "Unexpected hash failure: $($_.Exception.Message)"};Assert-True $closed "Wrong manifest hash must close";Assert-True ((Read-Invocations $log).Count-eq1) "Wrong hash leaked downstream";$env:NVIDEA_FAKE_VALIDATOR_FIXTURE=$validatorFixture
 $env:NVIDEA_FAKE_DOTNET_MALFORM_FRAGMENT="Nvidea.PersonalAiDemoEval.csproj";Remove-Item $log -Force -ErrorAction SilentlyContinue;$closed=$false;try{& $preflight -RepositoryRoot $repoRoot -ArtifactsDirectory "artifacts/preflight-behavior-semantic"}catch{$closed=$true;Assert-True $_.Exception.Message.Contains("not an explicit PASS") "Unexpected semantic failure"};Assert-True $closed "Non-PASS artifact must close";Assert-True ((Read-Invocations $log).Count-eq3) "Semantic failure reached adversarial evaluator"
 $env:NVIDEA_FAKE_DOTNET_MALFORM_FRAGMENT="__never_match__";Remove-Item $log -Force -ErrorAction SilentlyContinue;$successDir=Join-Path $repoRoot "artifacts/preflight-behavior-success";& $preflight -RepositoryRoot $repoRoot -ArtifactsDirectory $successDir;$calls=Read-Invocations $log;Assert-True ($calls.Count-eq4) "Success must execute four children";$expected=@("Nvidea.DemoPackageValidator.csproj","Nvidea.DemoChecklistGenerator.csproj","Nvidea.PersonalAiDemoEval.csproj","Nvidea.PersonalAiAdversarialEval.csproj");for($i=0;$i-lt4;$i++){Assert-True $calls[$i].Contains($expected[$i]) "Unexpected child order"};$joined=$calls-join"`n";foreach($f in @("Nvidea.JudgingEvidenceVerifier","NebiusLive","TavilyLive","PlaywrightLive")){Assert-True (-not $joined.Contains($f)) "Reached provider-live command $f"}
 $receiptPath=Join-Path $successDir "preflight-receipt.json";Assert-True (Test-Path -LiteralPath $receiptPath -PathType Leaf) "Run receipt missing";$receipt=Get-Content -LiteralPath $receiptPath -Raw|ConvertFrom-Json;Assert-True ($receipt.schemaVersion-eq1-and-not[string]::IsNullOrWhiteSpace([string]$receipt.runId)) "Receipt identity invalid";Assert-True ([string]$receipt.manifest.sha256-eq$manifestSha) "Receipt manifest binding invalid";Assert-True (@($receipt.artifacts).Count-eq4) "Receipt artifact binding incomplete";Assert-True (-not $receipt.providerLiveEvidence) "Local receipt must not claim provider-live evidence";foreach($a in @($receipt.artifacts)){$p=Join-Path $repoRoot ([string]$a.path);Assert-True ((Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()-eq[string]$a.sha256) "Receipt artifact hash mismatch"}
 Remove-Item $log -Force -ErrorAction SilentlyContinue;$closed=$false;try{& $preflight -RepositoryRoot $repoRoot -ArtifactsDirectory (Join-Path $temp "escape")}catch{$closed=$true;Assert-True $_.Exception.Message.Contains("ArtifactsDirectory must remain") "Unexpected escape failure"};Assert-True $closed "Escape must close";Assert-True ((Read-Invocations $log).Count-eq0) "Escape launched child"
 Write-Host "submission-preflight behavior PASS (hash/run-bound semantic fake-dotnet fault injection; network/provider free)."
}finally{foreach($n in $names){[Environment]::SetEnvironmentVariable($n,$old[$n])};Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue;foreach($n in @("failure","withheld","wronghash","semantic","success")){Remove-Item (Join-Path $repoRoot "artifacts/preflight-behavior-$n") -Recurse -Force -ErrorAction SilentlyContinue}}
