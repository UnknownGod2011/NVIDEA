[CmdletBinding()]
param(
    [switch]$RequireCloudResearch,
    [switch]$ValidateBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$failures = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

function Test-SecretPresence([string]$Name) {
    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        $script:failures.Add("Missing required environment variable: $Name")
    }
    # Deliberately never print, persist, hash, measure, or otherwise expose secret material.
}

function Test-ConfigPresence([string]$Name) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($Name))) {
        $script:failures.Add("Missing required configuration variable: $Name")
    }
}

function Test-ConfiguredFile([string]$Name) {
    $configuredPath = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($configuredPath)) {
        $script:failures.Add("Missing required file configuration variable: $Name")
        return
    }

    # Validate only filesystem metadata. Never read, hash, size, or print private-key material.
    try {
        $resolved = Resolve-Path -LiteralPath $configuredPath -ErrorAction Stop
        if (-not (Test-Path -LiteralPath $resolved.Path -PathType Leaf)) {
            $script:failures.Add("Configured file for $Name is not a regular file.")
        }
    }
    catch {
        $script:failures.Add("Configured file for $Name does not exist or is inaccessible.")
    }
}

if ($PSVersionTable.PSVersion.Major -lt 7) {
    $failures.Add('PowerShell 7+ is required for recording-day tooling.')
}
if (-not $IsWindows) {
    $failures.Add('The judge/demo desktop flow must be validated on Windows.')
}

$dotnetAvailable = $null -ne (Get-Command dotnet -ErrorAction SilentlyContinue)
if (-not $dotnetAvailable) {
    $failures.Add('dotnet is not available on PATH.')
}
else {
    $sdkLines = @(& dotnet --list-sdks 2>$null)
    if ($LASTEXITCODE -ne 0) {
        $failures.Add('dotnet --list-sdks failed.')
    }
    elseif (-not ($sdkLines | Where-Object { $_ -match '^8\.' })) {
        $failures.Add('.NET 8 SDK is required but was not found by dotnet --list-sdks.')
    }
}

$solution = Join-Path $repoRoot 'Nvidea.sln'
if (-not (Test-Path -LiteralPath $solution -PathType Leaf)) {
    $failures.Add('Nvidea.sln is missing from repository root.')
}

# Local judge path: the core model and Tavily research must both be genuinely configured.
Test-SecretPresence 'NEBIUS_API_KEY'
Test-SecretPresence 'TAVILY_API_KEY'

if ($RequireCloudResearch) {
    # Credential-free topology first. Names mirror NebiusResearchLiveConfigurationLoader.
    @(
        'NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT',
        'NVIDEA_LIVE_OBJECT_STORAGE_REGION',
        'NVIDEA_LIVE_SERVERLESS_PROJECT_ID',
        'NVIDEA_LIVE_WORKER_IMAGE',
        'NVIDEA_LIVE_SUBNET_ID',
        'NVIDEA_LIVE_PLATFORM',
        'NVIDEA_LIVE_PRESET',
        'NVIDEA_LIVE_TIMEOUT',
        'NVIDEA_LIVE_DISK_TYPE',
        'NVIDEA_LIVE_DISK_SIZE_BYTES',
        'NVIDEA_LIVE_TRANSPORT_SOURCE',
        'NVIDEA_LIVE_OBJECT_STORAGE_BUCKET',
        'NVIDEA_LIVE_SECRET_NEBIUS_API_KEY_ID',
        'NVIDEA_LIVE_SECRET_NEBIUS_API_KEY_VERSION_ID',
        'NVIDEA_LIVE_SECRET_TAVILY_API_KEY_ID',
        'NVIDEA_LIVE_SECRET_TAVILY_API_KEY_VERSION_ID',
        'NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_ID',
        'NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_VERSION_ID'
    ) | ForEach-Object { Test-ConfigPresence $_ }

    @(
        'NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE',
        'NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE',
        'NVIDEA_LIVE_CLIENT_RESULT_PRIVATE_KEY_PEM_FILE'
    ) | ForEach-Object { Test-ConfiguredFile $_ }

    # Provider credentials are presence-checked only after topology and key-file checks.
    @(
        'NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN',
        'NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID',
        'NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY'
    ) | ForEach-Object { Test-SecretPresence $_ }
}

# Optional compilation gate deliberately uses --no-restore so readiness cannot silently
# download packages or turn a local preflight into an unexpected network operation.
if ($ValidateBuild -and $dotnetAvailable -and (Test-Path -LiteralPath $solution -PathType Leaf)) {
    Write-Host 'Validating existing restored solution with dotnet build --no-restore...'
    & dotnet build $solution --no-restore --nologo --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        $failures.Add('dotnet build --no-restore failed. Restore dependencies explicitly before recording, then rerun readiness.')
    }
}

Write-Host "NVIDEA live demo readiness: $($failures.Count) blocker(s), $($warnings.Count) warning(s)."
foreach ($warning in $warnings) { Write-Warning $warning }
foreach ($failure in $failures) { Write-Error $failure -ErrorAction Continue }

if ($failures.Count -gt 0) { exit 1 }
Write-Host 'NVIDEA live demo readiness PASS (local configuration/build checks only; no provider/network calls were made by this script).'
exit 0
