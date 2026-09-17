[CmdletBinding()]
param(
    [switch]$RequireCloudResearch
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
        return
    }
    # Deliberately never print, persist, hash, measure, or otherwise expose secret material.
}

function Test-ConfigPresence([string]$Name) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($Name))) {
        $script:failures.Add("Missing required configuration variable: $Name")
    }
}

if ($PSVersionTable.PSVersion.Major -lt 7) {
    $failures.Add('PowerShell 7+ is required for recording-day tooling.')
}
if (-not $IsWindows) {
    $failures.Add('The judge/demo desktop flow must be validated on Windows.')
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    $failures.Add('dotnet is not available on PATH.')
}
else {
    $sdkText = (& dotnet --version 2>$null | Select-Object -First 1)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($sdkText)) {
        $failures.Add('dotnet --version failed.')
    }
    elseif ($sdkText -notmatch '^8\.') {
        $warnings.Add("Expected .NET 8 SDK for the current solution; found $sdkText.")
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
        'NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_VERSION_ID',
        'NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE',
        'NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE',
        'NVIDEA_LIVE_CLIENT_RESULT_PRIVATE_KEY_PEM_FILE'
    ) | ForEach-Object { Test-ConfigPresence $_ }

    # Provider credentials are presence-checked only after topology names.
    @(
        'NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN',
        'NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID',
        'NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY'
    ) | ForEach-Object { Test-SecretPresence $_ }
}

Write-Host "NVIDEA live demo readiness: $($failures.Count) blocker(s), $($warnings.Count) warning(s)."
foreach ($warning in $warnings) { Write-Warning $warning }
foreach ($failure in $failures) { Write-Error $failure -ErrorAction Continue }

if ($failures.Count -gt 0) { exit 1 }
Write-Host 'NVIDEA live demo readiness PASS (configuration presence only; no provider/network calls were made).'
exit 0
