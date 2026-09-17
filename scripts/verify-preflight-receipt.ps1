[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$ReceiptPath = 'artifacts/preflight/preflight-receipt.json'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-Sha256Hex { param([Parameter(Mandatory)][string]$Path) (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Assert-AllowedProperties {
    param([Parameter(Mandatory)]$Object,[Parameter(Mandatory)][string[]]$Allowed,[Parameter(Mandatory)][string]$Path)
    $set=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($n in $Allowed){$null=$set.Add($n)}
    foreach($p in $Object.PSObject.Properties){if(-not$set.Contains($p.Name)){throw "Receipt contains unexpected property '$($p.Name)' at $Path."}}
}
function Assert-NoDuplicateJsonProperties {
    param([Parameter(Mandatory)][System.Text.Json.JsonElement]$Element,[string]$Path='$')
    if($Element.ValueKind-eq[System.Text.Json.JsonValueKind]::Object){
        $names=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach($p in $Element.EnumerateObject()){
            if(-not$names.Add($p.Name)){throw "Receipt contains duplicate property '$($p.Name)' at $Path."}
            Assert-NoDuplicateJsonProperties $p.Value "$Path.$($p.Name)"
        }
    } elseif($Element.ValueKind-eq[System.Text.Json.JsonValueKind]::Array){$i=0;foreach($x in $Element.EnumerateArray()){Assert-NoDuplicateJsonProperties $x "$Path[$i]";$i++}}
}
function Resolve-RepositoryRelativeFile {
    param([Parameter(Mandatory)][string]$Relative,[Parameter(Mandatory)][string]$Root,[Parameter(Mandatory)][string]$Prefix)
    if([string]::IsNullOrWhiteSpace($Relative)-or[IO.Path]::IsPathRooted($Relative)){throw "Receipt artifact path must be repository-relative: '$Relative'."}
    if($Relative.Contains('\')){throw "Receipt paths must use canonical forward slashes: '$Relative'."}
    $full=[IO.Path]::GetFullPath((Join-Path $Root $Relative))
    if(-not$full.StartsWith($Prefix,[StringComparison]::OrdinalIgnoreCase)){throw "Receipt path escapes repository: '$Relative'."}
    if(-not(Test-Path -LiteralPath $full -PathType Leaf)){throw "Receipt-bound file is missing: '$Relative'."}
    $full
}

try{$null=[System.Text.Json.JsonDocumentOptions]::new()}catch{throw 'Receipt verification requires PowerShell with System.Text.Json available (PowerShell 7+ recommended).'}
$root=[IO.Path]::GetFullPath($RepositoryRoot);if(-not(Test-Path $root -PathType Container)){throw "Repository root does not exist: $root"}
$prefix=$root.TrimEnd([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar
$receiptFull=if([IO.Path]::IsPathRooted($ReceiptPath)){[IO.Path]::GetFullPath($ReceiptPath)}else{[IO.Path]::GetFullPath((Join-Path $root $ReceiptPath))}
if(-not$receiptFull.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Receipt must remain inside the repository.'}
if(-not(Test-Path -LiteralPath $receiptFull -PathType Leaf)){throw "Receipt is missing: $receiptFull"}
$item=Get-Item $receiptFull;if($item.Length-le0-or$item.Length-gt256KB){throw 'Receipt must be non-empty and no larger than 256 KiB.'}
$raw=Get-Content -LiteralPath $receiptFull -Raw -Encoding UTF8
try{$opts=[System.Text.Json.JsonDocumentOptions]::new();$opts.MaxDepth=32;$opts.AllowTrailingCommas=$false;$opts.CommentHandling=[System.Text.Json.JsonCommentHandling]::Disallow;$doc=[System.Text.Json.JsonDocument]::Parse($raw,$opts);try{Assert-NoDuplicateJsonProperties $doc.RootElement}finally{$doc.Dispose()};$r=$raw|ConvertFrom-Json -ErrorAction Stop}catch{throw "Receipt is invalid or ambiguous JSON: $($_.Exception.Message)"}
Assert-AllowedProperties $r @('schemaVersion','runId','startedAtUtc','completedAtUtc','manifest','artifacts','scope','providerLiveEvidence') '$'
Assert-AllowedProperties $r.manifest @('path','sha256') '$.manifest'
if([int]$r.schemaVersion-ne1){throw 'Receipt schemaVersion must be 1.'}
if([string]$r.runId-notmatch'^[0-9a-f]{32}$'){throw 'Receipt runId must be a lowercase GUID in N format.'}
$started=[DateTimeOffset]::MinValue;$completed=[DateTimeOffset]::MinValue
if(-not[DateTimeOffset]::TryParseExact([string]$r.startedAtUtc,'O',[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::RoundtripKind,[ref]$started)){throw 'Receipt startedAtUtc must be an ISO-8601 round-trip timestamp.'}
if(-not[DateTimeOffset]::TryParseExact([string]$r.completedAtUtc,'O',[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::RoundtripKind,[ref]$completed)){throw 'Receipt completedAtUtc must be an ISO-8601 round-trip timestamp.'}
if($completed-lt$started){throw 'Receipt completion precedes start.'}
if([string]$r.scope-ne'local-zero-cost'-or$r.providerLiveEvidence-isnot[bool]-or$r.providerLiveEvidence){throw 'Receipt must explicitly remain local-zero-cost with providerLiveEvidence=false.'}
if([string]$r.manifest.path-ne'docs/demo-package.json'){throw 'Receipt manifest path is not canonical.'}
$manifest=Resolve-RepositoryRelativeFile $r.manifest.path $root $prefix
if([string]$r.manifest.sha256-notmatch'^[0-9a-f]{64}$'-or-not[string]::Equals([string]$r.manifest.sha256,(Get-Sha256Hex $manifest),[StringComparison]::Ordinal)){throw 'Receipt manifest SHA-256 mismatch.'}
$expected=@('artifacts/preflight/demo-package-validation.json','artifacts/preflight/recording-checklist.md','artifacts/preflight/personal-ai-positive.json','artifacts/preflight/personal-ai-adversarial.json')
$artifacts=@($r.artifacts);if($artifacts.Count-ne$expected.Count){throw "Receipt must bind exactly $($expected.Count) artifacts."}
$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
for($i=0;$i-lt$artifacts.Count;$i++){
    $a=$artifacts[$i];Assert-AllowedProperties $a @('path','sha256','length') "$.artifacts[$i]"
    if([string]$a.path-ne$expected[$i]-or-not$seen.Add([string]$a.path)){throw "Receipt artifact order/path is unexpected at index $i."}
    $full=Resolve-RepositoryRelativeFile ([string]$a.path) $root $prefix;$actual=Get-Item $full
    if($a.length-isnot[long]-and$a.length-isnot[int]){throw "Receipt artifact length is malformed at index $i."}
    if([long]$a.length-le0-or[long]$a.length-ne[long]$actual.Length){throw "Receipt artifact length mismatch for '$($a.path)'."}
    if([string]$a.sha256-notmatch'^[0-9a-f]{64}$'-or-not[string]::Equals([string]$a.sha256,(Get-Sha256Hex $full),[StringComparison]::Ordinal)){throw "Receipt artifact SHA-256 mismatch for '$($a.path)'."}
}
Write-Host 'Preflight receipt verification PASS (local zero-cost scope).'
