[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$ArtifactsDirectory = "artifacts/preflight"
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-RepositoryFilePath {
    param([Parameter(Mandatory)][string]$Path,[Parameter(Mandatory)][string]$RepositoryPrefix)
    $full=[IO.Path]::GetFullPath($Path)
    if(-not $full.StartsWith($RepositoryPrefix,[StringComparison]::OrdinalIgnoreCase)){throw "Expected output must remain inside the repository: $full"}
    $full
}
function Get-Sha256Hex { param([Parameter(Mandatory)][string]$Path) (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Assert-JsonRuntimePrerequisites {
    try {
        $jsonDocumentType=[type]::GetType('System.Text.Json.JsonDocument, System.Text.Json',$false)
        $jsonElementType=[type]::GetType('System.Text.Json.JsonElement, System.Text.Json',$false)
        if($null-eq$jsonDocumentType-or$null-eq$jsonElementType){throw "System.Text.Json types are unavailable."}
        $null=[System.Text.Json.JsonDocumentOptions]::new()
    } catch {
        throw "Submission preflight requires PowerShell with System.Text.Json available (PowerShell 7+ on the supported .NET runtime is recommended). Install/launch a current PowerShell 7 environment, then rerun. Detail: $($_.Exception.Message)"
    }
}
function Assert-NoDuplicateJsonProperties {
    param([Parameter(Mandatory)][System.Text.Json.JsonElement]$Element,[Parameter(Mandatory)][string]$Label,[string]$JsonPath='$')
    if($Element.ValueKind-eq[System.Text.Json.JsonValueKind]::Object){
        $names=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach($p in $Element.EnumerateObject()){
            if(-not$names.Add($p.Name)){throw "$Label contains duplicate JSON property '$($p.Name)' at $JsonPath."}
            Assert-NoDuplicateJsonProperties -Element $p.Value -Label $Label -JsonPath "$JsonPath.$($p.Name)"
        }
    } elseif($Element.ValueKind-eq[System.Text.Json.JsonValueKind]::Array){$i=0;foreach($x in $Element.EnumerateArray()){Assert-NoDuplicateJsonProperties -Element $x -Label $Label -JsonPath "$JsonPath[$i]";$i++}}
}
function Assert-AllowedProperties {
    param([Parameter(Mandatory)]$Object,[Parameter(Mandatory)][string[]]$Allowed,[Parameter(Mandatory)][string]$Label,[Parameter(Mandatory)][string]$JsonPath)
    if($null-eq$Object-or$Object-isnot[psobject]){throw "$Label expected a JSON object at $JsonPath."}
    $allow=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($n in $Allowed){$null=$allow.Add($n)}
    foreach($p in $Object.PSObject.Properties){if(-not$allow.Contains($p.Name)){throw "$Label contains unexpected JSON property '$($p.Name)' at $JsonPath."}}
}
function Assert-EvidenceShape {
    param([Parameter(Mandatory)]$Evidence,[Parameter(Mandatory)][ValidateSet('validator','evaluator')][string]$Kind,[Parameter(Mandatory)][string]$Label)
    if($Kind-eq'validator'){$top=@('schemaVersion','overallPassed','manifestSha256','declaredMaximumDurationSeconds','plannedDurationSeconds','beatCount','checks');$check=@('id','passed','requirement')}
    else{$top=@('schemaVersion','generatedAt','overallPassed','checks','metrics');$check=@('id','passed','detail')}
    Assert-AllowedProperties -Object $Evidence -Allowed $top -Label $Label -JsonPath '$'
    if($null-ne$Evidence.checks){$i=0;foreach($c in @($Evidence.checks)){Assert-AllowedProperties -Object $c -Allowed $check -Label $Label -JsonPath "$.checks[$i]";$i++}}
    if($Kind-eq'evaluator'-and$null-ne$Evidence.metrics-and$Evidence.metrics-isnot[psobject]){throw "$Label expected metrics to be a JSON object."}
}
function Read-StrictEvidenceJson {
    param([Parameter(Mandatory)][string]$Path,[Parameter(Mandatory)][string]$Label)
    $item=Get-Item -LiteralPath $Path;if($item.Length-gt1MB){throw "$Label output exceeds the 1 MiB semantic-verification limit."}
    $raw=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    try{$o=[System.Text.Json.JsonDocumentOptions]::new();$o.AllowTrailingCommas=$false;$o.CommentHandling=[System.Text.Json.JsonCommentHandling]::Disallow;$o.MaxDepth=64;$d=[System.Text.Json.JsonDocument]::Parse($raw,$o);try{Assert-NoDuplicateJsonProperties -Element $d.RootElement -Label $Label}finally{$d.Dispose()};return($raw|ConvertFrom-Json -ErrorAction Stop)}catch{throw "$Label produced invalid or ambiguous JSON evidence: $($_.Exception.Message)"}
}
function Assert-PassedEvidence {
    param([Parameter(Mandatory)][string]$Path,[Parameter(Mandatory)][string]$Label,[Parameter(Mandatory)][int]$ExpectedSchemaVersion,[ValidateSet('validator','evaluator')][string]$Kind='evaluator')
    $e=Read-StrictEvidenceJson -Path $Path -Label $Label;Assert-EvidenceShape -Evidence $e -Kind $Kind -Label $Label
    if($null-eq$e.schemaVersion-or[int]$e.schemaVersion-ne$ExpectedSchemaVersion){throw "$Label produced an unexpected evidence schema version."}
    if($null-eq$e.overallPassed-or$e.overallPassed-isnot[bool]-or-not$e.overallPassed){throw "$Label produced evidence that is not an explicit PASS."}
    if($null-eq$e.checks-or@($e.checks).Count-eq0){throw "$Label produced no machine-verifiable checks."}
    foreach($c in @($e.checks)){if([string]::IsNullOrWhiteSpace([string]$c.id)-or$c.passed-isnot[bool]-or-not$c.passed){throw "$Label contains a missing, malformed, or failed check."}}
    $e
}
function Assert-ValidatorEvidence {
    param([Parameter(Mandatory)][string]$Path,[Parameter(Mandatory)][string]$ManifestPath)
    $e=Assert-PassedEvidence -Path $Path -Label 'Demo package validator' -ExpectedSchemaVersion 2 -Kind validator;$m=Read-StrictEvidenceJson -Path $ManifestPath -Label 'Demo manifest';$sha=Get-Sha256Hex $ManifestPath
    if([string]::IsNullOrWhiteSpace([string]$e.manifestSha256)-or-not[string]::Equals([string]$e.manifestSha256,$sha,[StringComparison]::OrdinalIgnoreCase)){throw 'Validator evidence manifest SHA-256 does not match the canonical manifest.'}
    if($null-eq$m.beats-or[int]$e.beatCount-ne@($m.beats).Count){throw 'Validator evidence beat count does not match the canonical manifest.'}
    if([int]$e.plannedDurationSeconds-ne(@($m.beats)|Measure-Object -Property durationSeconds -Sum).Sum){throw 'Validator evidence duration does not match the canonical manifest.'}
}
function Assert-ChecklistMatchesManifest {
    param([Parameter(Mandatory)][string]$Path,[Parameter(Mandatory)][string]$ManifestPath)
    $m=Read-StrictEvidenceJson $ManifestPath 'Demo manifest';$text=Get-Content -LiteralPath $Path -Raw -Encoding UTF8;$matches=[regex]::Matches($text,'(?m)^##\s+(\d+)\.\s+(.+?)\s+\((\d+)s\)\s*$')
    if($matches.Count-ne@($m.beats).Count){throw 'Generated checklist beat count does not match the canonical manifest.'}
    for($i=0;$i-lt@($m.beats).Count;$i++){$b=@($m.beats)[$i];if([int]$matches[$i].Groups[1].Value-ne($i+1)-or$matches[$i].Groups[2].Value-ne[string]$b.label-or[int]$matches[$i].Groups[3].Value-ne[int]$b.durationSeconds){throw "Generated checklist beat $($i+1) does not match the canonical manifest order/label/duration."};foreach($x in @($b.expectedSessionMilestones)){if(-not$text.Contains("``$x``",[StringComparison]::Ordinal)){throw "Generated checklist is missing expected milestone '$x'."}}}
    if(-not$text.Contains('## Take acceptance gate',[StringComparison]::Ordinal)){throw 'Generated checklist is missing its take acceptance gate.'}
}
function Invoke-CheckedDotnet {
    param([Parameter(Mandatory)][string[]]$Arguments,[Parameter(Mandatory)][string]$Label,[Parameter(Mandatory)][string]$ExpectedOutput,[Parameter(Mandatory)][string]$RepositoryPrefix)
    $out=Assert-RepositoryFilePath $ExpectedOutput $RepositoryPrefix;Remove-Item -LiteralPath $out -Force -ErrorAction SilentlyContinue;Write-Host "==> $Label";& dotnet @Arguments;if($LASTEXITCODE-ne0){throw "$Label failed with exit code $LASTEXITCODE."};if(-not(Test-Path -LiteralPath $out -PathType Leaf)){throw "$Label returned success but did not materialize expected output: $out"};if((Get-Item $out).Length-le0){throw "$Label returned success but materialized an empty output: $out"}
}

Assert-JsonRuntimePrerequisites
$root=[IO.Path]::GetFullPath($RepositoryRoot);if(-not(Test-Path $root -PathType Container)){throw "Repository root does not exist: $root"};$manifest=Join-Path $root 'docs/demo-package.json';if(-not(Test-Path $manifest -PathType Leaf)){throw "Demo manifest is missing: $manifest"};if(-not(Get-Command dotnet -ErrorAction SilentlyContinue)){throw '.NET 8 SDK is required for submission preflight.'}
$artifacts=if([IO.Path]::IsPathRooted($ArtifactsDirectory)){[IO.Path]::GetFullPath($ArtifactsDirectory)}else{[IO.Path]::GetFullPath((Join-Path $root $ArtifactsDirectory))};$rootPrefix=$root.TrimEnd([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar;if(-not$artifacts.StartsWith($rootPrefix,[StringComparison]::OrdinalIgnoreCase)){throw 'ArtifactsDirectory must remain inside the repository.'};New-Item -ItemType Directory -Path $artifacts -Force|Out-Null
$runId=[Guid]::NewGuid().ToString('N');$started=[DateTimeOffset]::UtcNow;Push-Location $root
try{
 $validation=Join-Path $artifacts 'demo-package-validation.json';Invoke-CheckedDotnet @('run','--project','tools/Nvidea.DemoPackageValidator/Nvidea.DemoPackageValidator.csproj','--','.','docs/demo-package.json','--output',$validation) 'Validate schema-v2 demo package' $validation $rootPrefix;Assert-ValidatorEvidence $validation $manifest
 $checklist=Join-Path $artifacts 'recording-checklist.md';Invoke-CheckedDotnet @('run','--project','tools/Nvidea.DemoChecklistGenerator/Nvidea.DemoChecklistGenerator.csproj','--','docs/demo-package.json','--output',$checklist) 'Generate validated recording checklist' $checklist $rootPrefix;Assert-ChecklistMatchesManifest $checklist $manifest
 $positive=Join-Path $artifacts 'personal-ai-positive.json';Invoke-CheckedDotnet @('run','--project','tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj','--','--output',$positive) 'Run positive Personal AI evaluator' $positive $rootPrefix;Assert-PassedEvidence $positive 'Positive Personal AI evaluator' 1 evaluator|Out-Null
 $adversarial=Join-Path $artifacts 'personal-ai-adversarial.json';Invoke-CheckedDotnet @('run','--project','tools/Nvidea.PersonalAiAdversarialEval/Nvidea.PersonalAiAdversarialEval.csproj','--','--output',$adversarial) 'Run adversarial Personal AI evaluator' $adversarial $rootPrefix;Assert-PassedEvidence $adversarial 'Adversarial Personal AI evaluator' 1 evaluator|Out-Null
 $receipt=Assert-RepositoryFilePath (Join-Path $artifacts 'preflight-receipt.json') $rootPrefix;Remove-Item $receipt -Force -ErrorAction SilentlyContinue;$bound=@();foreach($p in @($validation,$checklist,$positive,$adversarial)){$item=Get-Item $p;$bound+=@{path=[IO.Path]::GetRelativePath($root,$p).Replace('\','/');sha256=Get-Sha256Hex $p;length=[long]$item.Length}}
 @{schemaVersion=1;runId=$runId;startedAtUtc=$started.ToString('O');completedAtUtc=[DateTimeOffset]::UtcNow.ToString('O');manifest=@{path='docs/demo-package.json';sha256=Get-Sha256Hex $manifest};artifacts=$bound;scope='local-zero-cost';providerLiveEvidence=$false}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $receipt -Encoding UTF8;if((Get-Item $receipt).Length-le0){throw 'Preflight receipt was not materialized.'}
 Write-Host '==> Provider-live checks skipped (zero-cost/fail-closed mode).';Write-Host 'Submission preflight PASS (local zero-cost scope).';Write-Host "Run-bound receipt: $receipt"
}finally{Pop-Location}
