[CmdletBinding()]
param(
    [switch]$InstallChromium
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($PSVersionTable.PSVersion.Major -lt 7) {
    throw 'PowerShell 7+ is required for release browser qualification.'
}
if (-not $IsWindows) {
    throw 'Release browser qualification must run on Windows.'
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$runner = Join-Path $PSScriptRoot 'run-browser-integration.ps1'
if (-not (Test-Path -LiteralPath $runner -PathType Leaf)) {
    throw 'run-browser-integration.ps1 is missing; release qualification is blocked.'
}

$git = (Get-Command git -ErrorAction Stop).Source
$origin = (& $git -C $repoRoot remote get-url origin 2>$null).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Could not resolve the Git origin for the NVIDEA checkout.'
}
$normalizedOrigin = $origin.TrimEnd('/').ToLowerInvariant()
$allowedOrigins = @(
    'https://github.com/unknowngod2011/nvidea.git',
    'https://github.com/unknowngod2011/nvidea',
    'git@github.com:unknowngod2011/nvidea.git'
)
if ($normalizedOrigin -notin $allowedOrigins) {
    throw "Release qualification is pinned to UnknownGod2011/NVIDEA; current origin is '$origin'."
}

$head = (& $git -C $repoRoot rev-parse --verify HEAD 2>$null).Trim()
if ($LASTEXITCODE -ne 0 -or $head -notmatch '^[0-9a-fA-F]{40}$') {
    throw 'Could not resolve an exact 40-hex HEAD commit; refusing provenance-free release evidence.'
}

$dirty = @(& $git -C $repoRoot status --porcelain --untracked-files=normal 2>$null)
if ($LASTEXITCODE -ne 0) {
    throw 'Could not verify NVIDEA working-tree cleanliness; release qualification is blocked.'
}
if ($dirty.Count -gt 0) {
    throw 'Release browser qualification requires a clean NVIDEA checkout. Commit/stash changes and re-run.'
}

Write-Host "Qualifying clean NVIDEA source commit $head with the canonical real-Chromium security suite..."
$pwsh = (Get-Command pwsh -ErrorAction Stop).Source
$args = @('-NoLogo', '-NoProfile', '-File', $runner, '-SecuritySuite', '-KeepResults')
if ($InstallChromium) { $args += '-InstallChromium' }
& $pwsh @args
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    throw "Release browser qualification failed with exit code $exitCode. Do not use its evidence for judge recording."
}

Write-Host 'Release browser qualification PASS. Retain the emitted evidence directory and pass it to judge-recording-gate.ps1.'
exit 0
