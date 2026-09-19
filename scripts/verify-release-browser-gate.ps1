[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$EvidenceDirectory,
    [ValidateRange(1, 168)]
    [int]$MaxEvidenceAgeHours = 24
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$verifier = Join-Path $PSScriptRoot 'verify-browser-qualification.ps1'
if (-not (Test-Path -LiteralPath $verifier -PathType Leaf)) { throw "Browser qualification verifier not found: $verifier" }

$git = Get-Command git -ErrorAction Stop
$inside = (& $git.Source -C $repoRoot rev-parse --is-inside-work-tree 2>$null | Select-Object -First 1)
if ($LASTEXITCODE -ne 0 -or $inside -ne 'true') { throw 'Release browser gate must run from a valid Git checkout.' }

$remote = (& $git.Source -C $repoRoot config --get remote.origin.url 2>$null | Select-Object -First 1)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($remote)) { throw 'Cannot determine origin remote for release qualification.' }
$normalizedRemote = ([string]$remote).Trim().TrimEnd('/')
$allowedRemotes = @(
    'https://github.com/UnknownGod2011/NVIDEA.git',
    'https://github.com/UnknownGod2011/NVIDEA',
    'git@github.com:UnknownGod2011/NVIDEA.git',
    'ssh://git@github.com/UnknownGod2011/NVIDEA.git'
)
if ($normalizedRemote -notin $allowedRemotes) { throw "Release browser gate refuses unexpected origin remote '$normalizedRemote'." }

$head = (& $git.Source -C $repoRoot rev-parse HEAD 2>$null | Select-Object -First 1)
if ($LASTEXITCODE -ne 0 -or [string]$head -notmatch '^[0-9a-fA-F]{40}$') { throw 'Cannot resolve a full HEAD commit for release qualification.' }
$head = ([string]$head).Trim().ToLowerInvariant()

$status = @(& $git.Source -C $repoRoot status --porcelain=v1 --untracked-files=all 2>$null)
if ($LASTEXITCODE -ne 0) { throw 'Cannot determine working-tree cleanliness for release qualification.' }
if ($status.Count -gt 0) { throw 'Release browser gate requires a clean working tree so evidence is tied to committed source.' }

Write-Host "Release browser gate: verifying fresh canonical security evidence for commit $head"
& $verifier `
    -EvidenceDirectory $EvidenceDirectory `
    -ExpectedCommit $head `
    -RequireCleanSource `
    -RequireSecuritySuite `
    -MaxEvidenceAgeHours $MaxEvidenceAgeHours
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Release browser qualification gate PASS.'
Write-Host "  Repository: UnknownGod2011/NVIDEA"
Write-Host "  Commit: $head"
Write-Host "  Maximum evidence age: $MaxEvidenceAgeHours hour(s)"
exit 0
