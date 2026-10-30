[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9._-]+$')][string]$RuntimeIdentifier = 'win-x64',
    [Parameter(Mandatory)][string]$OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'src/Nvidea.Windows/Nvidea.Windows.csproj'
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (-not $output.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputDirectory must remain inside this repository.'
}

dotnet publish $project -c Release -r $RuntimeIdentifier --self-contained true -p:PublishSingleFile=false -p:DebugType=None -o $output
if ($LASTEXITCODE -ne 0) { throw 'Windows publish failed.' }
Write-Host "NVIDEA Windows release output created at: $output"
