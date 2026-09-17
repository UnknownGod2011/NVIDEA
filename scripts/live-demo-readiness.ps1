[CmdletBinding()]
param(
    [switch]$RequireCloudResearch,
    [switch]$ValidateBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$resolvedRepoRoot = (Resolve-Path -LiteralPath $repoRoot -ErrorAction Stop).Path.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
$repoPrefix = $resolvedRepoRoot + [IO.Path]::DirectorySeparatorChar
$failures = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

function Test-SecretPresence([string]$Name) {
    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) { $script:failures.Add("Missing required environment variable: $Name") }
}

function Test-ConfigPresence([string]$Name) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($Name))) { $script:failures.Add("Missing required configuration variable: $Name") }
}

function Test-ConfiguredFile([string]$Name) {
    $configuredPath = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($configuredPath)) {
        $script:failures.Add("Missing required file configuration variable: $Name")
        return
    }
    try {
        $resolved = Resolve-Path -LiteralPath $configuredPath -ErrorAction Stop
        if (-not (Test-Path -LiteralPath $resolved.Path -PathType Leaf)) { $script:failures.Add("Configured file for $Name is not a regular file.") }
    }
    catch { $script:failures.Add("Configured file for $Name does not exist or is inaccessible.") }
}

function Test-RepositoryPath([string]$RelativePath, [string]$Description, [switch]$Leaf) {
    if ([string]::IsNullOrWhiteSpace($RelativePath) -or [IO.Path]::IsPathRooted($RelativePath)) {
        $script:failures.Add("$Description must be a non-empty repository-relative path.")
        return $false
    }

    # First enforce lexical confinement, then resolve the existing target and enforce
    # confinement again. The second check is required because Windows junctions and
    # symlinks can make an apparently in-repository path resolve outside the checkout.
    try { $candidate = [IO.Path]::GetFullPath((Join-Path $repoRoot $RelativePath)) }
    catch {
        $script:failures.Add("$Description is not a valid repository-relative path.")
        return $false
    }
    $lexicalPrefix = $repoRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $candidate.StartsWith($lexicalPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        $script:failures.Add("$Description escapes the repository boundary.")
        return $false
    }

    try { $resolvedCandidate = (Resolve-Path -LiteralPath $candidate -ErrorAction Stop).Path }
    catch {
        $script:failures.Add("$Description does not exist in the repository.")
        return $false
    }
    if (-not $resolvedCandidate.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        $script:failures.Add("$Description resolves outside the repository boundary.")
        return $false
    }

    $pathType = if ($Leaf) { 'Leaf' } else { 'Any' }
    if (-not (Test-Path -LiteralPath $resolvedCandidate -PathType $pathType)) {
        $script:failures.Add("$Description does not exist in the repository.")
        return $false
    }
    return $true
}

function Test-DemoPackage {
    $manifestPath = Join-Path $repoRoot 'docs/demo-package.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { $script:failures.Add('docs/demo-package.json is missing.'); return }
    try { $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 32 -ErrorAction Stop }
    catch { $script:failures.Add('docs/demo-package.json is not valid JSON.'); return }

    if ($manifest.schemaVersion -ne 2) { $script:failures.Add('Demo package schemaVersion must remain 2 for the current judge evidence contract.') }
    if ($manifest.maxDurationSeconds -gt 180) { $script:failures.Add('Demo package exceeds the hackathon 180-second recording limit.') }
    if ($null -eq $manifest.beats -or $manifest.beats.Count -eq 0) { $script:failures.Add('Demo package has no demo beats.'); return }

    $duration = 0
    $observedMilestones = [System.Collections.Generic.List[string]]::new()
    $seenBeatIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($beat in $manifest.beats) {
        if ([string]::IsNullOrWhiteSpace([string]$beat.id) -or -not $seenBeatIds.Add([string]$beat.id)) { $script:failures.Add('Demo package beat IDs must be non-empty and unique.') }
        if ([int]$beat.durationSeconds -le 0) { $script:failures.Add("Demo beat '$($beat.id)' must have a positive duration.") }
        $duration += [int]$beat.durationSeconds
        foreach ($milestone in @($beat.expectedSessionMilestones)) { $observedMilestones.Add([string]$milestone) }
        foreach ($featurePath in @($beat.featurePaths)) { [void](Test-RepositoryPath ([string]$featurePath) "Demo beat '$($beat.id)' feature path") }
        foreach ($evidence in @($beat.evidence)) {
            if ([string]::IsNullOrWhiteSpace([string]$evidence.evidenceClass) -or [string]::IsNullOrWhiteSpace([string]$evidence.claim)) {
                $script:failures.Add("Demo beat '$($beat.id)' has evidence without an explicit class and claim.")
            }
            [void](Test-RepositoryPath ([string]$evidence.path) "Demo beat '$($beat.id)' evidence path" -Leaf)
        }
    }

    if ($duration -gt [int]$manifest.maxDurationSeconds -or $duration -gt 180) { $script:failures.Add("Demo beat duration totals $duration seconds, exceeding the declared or hackathon limit.") }

    $expectedMilestones = @('NemotronInferenceCompleted','MemoryInfluencedResponse','TavilyValidatedCitationUsed','BrowserVerifiedGoalCompleted','ConsequentialApprovalGranted','NebiusBackgroundExecutionObserved')
    if (($observedMilestones -join '|') -cne ($expectedMilestones -join '|')) { $script:failures.Add('Demo package runtime milestone sequence drifted from the six production-observed judge beats.') }

    if ($null -eq $manifest.commands -or $manifest.commands.Count -eq 0) {
        $script:failures.Add('Demo package has no verification commands.')
        return
    }
    $seenCommandLabels = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($command in $manifest.commands) {
        if ([string]::IsNullOrWhiteSpace([string]$command.label) -or -not $seenCommandLabels.Add([string]$command.label)) { $script:failures.Add('Demo verification command labels must be non-empty and unique.') }
        [void](Test-RepositoryPath ([string]$command.projectPath) "Demo verification command '$($command.label)' projectPath" -Leaf)
        if ([string]::IsNullOrWhiteSpace([string]$command.command)) { $script:failures.Add("Demo verification command '$($command.label)' has no command text.") }
        elseif (-not ([string]$command.command).Contains([string]$command.projectPath, [StringComparison]::Ordinal)) { $script:failures.Add("Demo verification command '$($command.label)' does not reference its declared projectPath.") }
    }
}

if ($PSVersionTable.PSVersion.Major -lt 7) { $failures.Add('PowerShell 7+ is required for recording-day tooling.') }
if (-not $IsWindows) { $failures.Add('The judge/demo desktop flow must be validated on Windows.') }

$dotnetAvailable = $null -ne (Get-Command dotnet -ErrorAction SilentlyContinue)
if (-not $dotnetAvailable) { $failures.Add('dotnet is not available on PATH.') }
else {
    $sdkLines = @(& dotnet --list-sdks 2>$null)
    if ($LASTEXITCODE -ne 0) { $failures.Add('dotnet --list-sdks failed.') }
    elseif (-not ($sdkLines | Where-Object { $_ -match '^8\.' })) { $failures.Add('.NET 8 SDK is required but was not found by dotnet --list-sdks.') }
}

$solution = Join-Path $repoRoot 'Nvidea.sln'
if (-not (Test-Path -LiteralPath $solution -PathType Leaf)) { $failures.Add('Nvidea.sln is missing from repository root.') }

Test-DemoPackage
Test-SecretPresence 'NEBIUS_API_KEY'
Test-SecretPresence 'TAVILY_API_KEY'

if ($RequireCloudResearch) {
    @('NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT','NVIDEA_LIVE_OBJECT_STORAGE_REGION','NVIDEA_LIVE_SERVERLESS_PROJECT_ID','NVIDEA_LIVE_WORKER_IMAGE','NVIDEA_LIVE_SUBNET_ID','NVIDEA_LIVE_PLATFORM','NVIDEA_LIVE_PRESET','NVIDEA_LIVE_TIMEOUT','NVIDEA_LIVE_DISK_TYPE','NVIDEA_LIVE_DISK_SIZE_BYTES','NVIDEA_LIVE_TRANSPORT_SOURCE','NVIDEA_LIVE_OBJECT_STORAGE_BUCKET','NVIDEA_LIVE_SECRET_NEBIUS_API_KEY_ID','NVIDEA_LIVE_SECRET_NEBIUS_API_KEY_VERSION_ID','NVIDEA_LIVE_SECRET_TAVILY_API_KEY_ID','NVIDEA_LIVE_SECRET_TAVILY_API_KEY_VERSION_ID','NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_ID','NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_VERSION_ID') | ForEach-Object { Test-ConfigPresence $_ }
    @('NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE','NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE','NVIDEA_LIVE_CLIENT_RESULT_PRIVATE_KEY_PEM_FILE') | ForEach-Object { Test-ConfiguredFile $_ }
    @('NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN','NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID','NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY') | ForEach-Object { Test-SecretPresence $_ }
}

if ($ValidateBuild -and $dotnetAvailable -and (Test-Path -LiteralPath $solution -PathType Leaf)) {
    Write-Host 'Validating existing restored solution with dotnet build --no-restore...'
    & dotnet build $solution --no-restore --nologo --verbosity minimal
    if ($LASTEXITCODE -ne 0) { $failures.Add('dotnet build --no-restore failed. Restore dependencies explicitly before recording, then rerun readiness.') }
}

Write-Host "NVIDEA live demo readiness: $($failures.Count) blocker(s), $($warnings.Count) warning(s)."
foreach ($warning in $warnings) { Write-Warning $warning }
foreach ($failure in $failures) { Write-Error $failure -ErrorAction Continue }
if ($failures.Count -gt 0) { exit 1 }
Write-Host 'NVIDEA live demo readiness PASS (local configuration/build/demo-contract checks only; no provider/network calls were made by this script).'
exit 0
