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

if (Test-Path -LiteralPath $output) {
    if (Get-ChildItem -LiteralPath $output -Force | Select-Object -First 1) {
        throw 'OutputDirectory must be a new or empty directory. Existing artifacts are never overwritten.'
    }
}

$archive = "$output.zip"
if (Test-Path -LiteralPath $archive) {
    throw "The release archive already exists and will not be overwritten: $archive"
}

New-Item -ItemType Directory -Path $output -Force | Out-Null

dotnet publish $project -c Release -r $RuntimeIdentifier --self-contained true -p:PublishSingleFile=false -p:DebugType=None -o $output
if ($LASTEXITCODE -ne 0) { throw 'Windows publish failed.' }

$internalAppHost = Join-Path $output 'Nvidea.Windows.exe'
$brandedAppHost = Join-Path $output 'keyboard-wtf.exe'
if (-not (Test-Path -LiteralPath $internalAppHost -PathType Leaf)) {
    throw 'The published Windows app host was not found.'
}
if (Test-Path -LiteralPath $brandedAppHost) {
    throw 'A branded launcher already exists; refusing to overwrite it.'
}
Move-Item -LiteralPath $internalAppHost -Destination $brandedAppHost

$releaseReadme = @'
keyboard-wtf — Windows x64 portable build

1. Extract the entire ZIP into a folder.
2. Run keyboard-wtf.exe.
3. Local Dictation uses Windows speech recognition and works without a cloud API key.
4. Smart Dictation and Jarvis require a Nebius Token Factory API key. Open Settings > Models & keys and enter it there.

Get a Token Factory key at https://tokenfactory.nebius.com/:
sign in, select the project to bill, open API keys, then select Get API key.
Copy the key when it is shown; Nebius does not display it again. Never put it in chat, screenshots, or source files.

Saved provider keys are protected for the current Windows user with DPAPI. Model and key changes apply after restarting keyboard-wtf. Tavily is optional and only needed for web research.

Voice privacy: microphone audio is recognized locally and is not sent to Nebius. Smart Dictation and Jarvis send transcript text to Nemotron through Nebius Token Factory. Jarvis is turn-taking: it listens, then streams a response and speaks locally. It is not full-duplex audio.

Configure global shortcuts in Settings > Shortcuts. Windows microphone permission and an installed desktop speech recognizer are required for voice input.

This is an unsigned portable build. Verify the source and release record before use. Nebius inference usage is billed to the selected project.
'@
[IO.File]::WriteAllText((Join-Path $output 'README.txt'), $releaseReadme, [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination (Join-Path $output 'LICENSE')

Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory(
    $output,
    $archive,
    [IO.Compression.CompressionLevel]::Optimal,
    $false)

$zip = [IO.Compression.ZipFile]::OpenRead($archive)
try {
    $requiredEntries = @('keyboard-wtf.exe', 'Nvidea.Windows.dll', 'README.txt', 'LICENSE')
    $entryNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $zip.Entries) { [void]$entryNames.Add($entry.FullName) }
    foreach ($requiredEntry in $requiredEntries) {
        if (-not $entryNames.Contains($requiredEntry)) {
            throw "The release archive is missing required file '$requiredEntry'."
        }
    }
}
finally {
    $zip.Dispose()
}

Write-Host "keyboard-wtf Windows portable folder created at: $output"
Write-Host "GitHub Release upload asset created at: $archive"
