[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'

$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$sourcePreflight=Join-Path $repoRoot 'scripts/submission-preflight.ps1'
$sourceVerifier=Join-Path $repoRoot 'scripts/verify-preflight-receipt.ps1'
$manifestPath=Join-Path $repoRoot 'docs/demo-package.json'
foreach($p in @($sourcePreflight,$sourceVerifier,$manifestPath)){if(-not(Test-Path -LiteralPath $p -PathType Leaf)){throw "Required production input missing: $p"}}
function Assert-True([bool]$Condition,[string]$Message){if(-not$Condition){throw $Message}}

$temp=Join-Path ([IO.Path]::GetTempPath()) ('nvidea-receipt-tamper-'+[Guid]::NewGuid().ToString('N'))
$isolated=Join-Path $temp 'repo';$scripts=Join-Path $isolated 'scripts';$docs=Join-Path $isolated 'docs';$bin=Join-Path $temp 'bin'
New-Item -ItemType Directory -Force -Path $scripts,$docs,$bin|Out-Null
Copy-Item -LiteralPath $sourcePreflight -Destination (Join-Path $scripts 'submission-preflight.ps1')
Copy-Item -LiteralPath $sourceVerifier -Destination (Join-Path $scripts 'verify-preflight-receipt.real.ps1')
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $docs 'demo-package.json')
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json;$duration=(@($manifest.beats)|Measure-Object durationSeconds -Sum).Sum;$manifestSha=(Get-FileHash -LiteralPath (Join-Path $docs 'demo-package.json') -Algorithm SHA256).Hash.ToLowerInvariant()
$validator=Join-Path $temp 'validator.json';@{schemaVersion=2;overallPassed=$true;manifestSha256=$manifestSha;declaredMaximumDurationSeconds=[int]$manifest.maxDurationSeconds;plannedDurationSeconds=[int]$duration;beatCount=@($manifest.beats).Count;checks=@(@{id='fixture';passed=$true;requirement='tamper harness'})}|ConvertTo-Json -Depth 8|Set-Content $validator -Encoding UTF8
$eval=Join-Path $temp 'eval.json';@{schemaVersion=1;generatedAt='2026-09-17T00:00:00Z';overallPassed=$true;checks=@(@{id='fixture';passed=$true;detail='tamper harness'})}|ConvertTo-Json -Depth 8|Set-Content $eval -Encoding UTF8
$checklist=Join-Path $temp 'checklist.md';$lines=@('# Generated Judge Demo Checklist','');for($i=0;$i-lt@($manifest.beats).Count;$i++){$b=@($manifest.beats)[$i];$lines+="## $($i+1). $($b.label) ($($b.durationSeconds)s)";foreach($m in @($b.expectedSessionMilestones)){$lines+="- [ ] ``$m`` is visible only after genuine production observation."}};$lines+='## Take acceptance gate';Set-Content $checklist -Value $lines -Encoding UTF8
@'
@echo off
setlocal
set "args=%*"
:scan
if "%~1"=="" exit /b 43
if /I "%~1"=="--output" goto output
shift
goto scan
:output
shift
if "%~1"=="" exit /b 44
echo %args% | findstr /C:"Nvidea.DemoPackageValidator.csproj" >nul && copy /Y "%NVIDEA_TAMPER_VALIDATOR%" "%~1" >nul && exit /b 0
echo %args% | findstr /C:"Nvidea.DemoChecklistGenerator.csproj" >nul && copy /Y "%NVIDEA_TAMPER_CHECKLIST%" "%~1" >nul && exit /b 0
copy /Y "%NVIDEA_TAMPER_EVAL%" "%~1" >nul
exit /b 0
'@|Set-Content (Join-Path $bin 'dotnet.cmd') -Encoding Ascii
@'
param([string]$RepositoryRoot,[string]$ReceiptPath)
$ErrorActionPreference='Stop'
$mode=$env:NVIDEA_RECEIPT_TAMPER_MODE
if($mode-ne'none'){
 $r=Get-Content -LiteralPath $ReceiptPath -Raw|ConvertFrom-Json
 switch($mode){
  'receipt-sha' {$r.artifacts[0].sha256='0'*64;$r|ConvertTo-Json -Depth 8|Set-Content $ReceiptPath -Encoding UTF8}
  'receipt-length' {$r.artifacts[0].length=[long]$r.artifacts[0].length+1;$r|ConvertTo-Json -Depth 8|Set-Content $ReceiptPath -Encoding UTF8}
  'scope' {$r.scope='provider-live';$r|ConvertTo-Json -Depth 8|Set-Content $ReceiptPath -Encoding UTF8}
  'provider-live' {$r.providerLiveEvidence=$true;$r|ConvertTo-Json -Depth 8|Set-Content $ReceiptPath -Encoding UTF8}
  'path' {$r.artifacts[0].path='../escape.json';$r|ConvertTo-Json -Depth 8|Set-Content $ReceiptPath -Encoding UTF8}
  'artifact-content' {$p=Join-Path $RepositoryRoot ([string]$r.artifacts[0].path);Add-Content -LiteralPath $p -Value 'tampered'}
  default {throw "Unknown tamper mode: $mode"}
 }
}
& (Join-Path $PSScriptRoot 'verify-preflight-receipt.real.ps1') -RepositoryRoot $RepositoryRoot -ReceiptPath $ReceiptPath
if(-not $?){exit 91}
'@|Set-Content (Join-Path $scripts 'verify-preflight-receipt.ps1') -Encoding UTF8
$names=@('PATH','NVIDEA_TAMPER_VALIDATOR','NVIDEA_TAMPER_CHECKLIST','NVIDEA_TAMPER_EVAL','NVIDEA_RECEIPT_TAMPER_MODE');$old=@{};foreach($n in $names){$old[$n]=[Environment]::GetEnvironmentVariable($n)}
try{
 $env:PATH=$bin+[IO.Path]::PathSeparator+$old.PATH;$env:NVIDEA_TAMPER_VALIDATOR=$validator;$env:NVIDEA_TAMPER_CHECKLIST=$checklist;$env:NVIDEA_TAMPER_EVAL=$eval
 $preflight=Join-Path $scripts 'submission-preflight.ps1'
 foreach($mode in @('receipt-sha','receipt-length','scope','provider-live','path','artifact-content')){$env:NVIDEA_RECEIPT_TAMPER_MODE=$mode;$output=@();$failed=$false;try{$output=@(& $preflight -RepositoryRoot $isolated -ArtifactsDirectory "artifacts/$mode" 2>&1)}catch{$failed=$true;$output+=@($_.Exception.Message)};Assert-True $failed "Tamper mode '$mode' must fail closed.";Assert-True (-not(($output|Out-String).Contains('Submission preflight PASS'))) "Tamper mode '$mode' reached PASS."}
 $env:NVIDEA_RECEIPT_TAMPER_MODE='none';$controlDir='artifacts/custom-receipt-control';$output=@(& $preflight -RepositoryRoot $isolated -ArtifactsDirectory $controlDir 2>&1);Assert-True (($output|Out-String).Contains('Submission preflight PASS')) 'Untampered custom-directory control did not PASS.';Assert-True (Test-Path -LiteralPath (Join-Path $isolated "$controlDir/preflight-receipt.json") -PathType Leaf) 'Control receipt missing.'
 Write-Host 'preflight receipt tamper behavior PASS (isolated production preflight + untouched verifier; network/provider free).'
}finally{foreach($n in $names){[Environment]::SetEnvironmentVariable($n,$old[$n])};Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue}
