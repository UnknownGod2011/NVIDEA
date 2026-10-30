# Judge setup: NVIDIA/Nebius Personal AI on Windows

NVIDEA is a Windows-first .NET 8 app. It uses NVIDIA Nemotron through Nebius Token Factory for cloud reasoning and Tavily for optional web research.

## Standard desktop path

Install the .NET 8 SDK, clone the repository, then open PowerShell in the repository root:

```powershell
$env:NEBIUS_API_KEY = "your-nebius-token-factory-key"
$env:TAVILY_API_KEY = "your-tavily-key"
dotnet restore .\src\Nvidea.Windows\Nvidea.Windows.csproj
dotnet run --project .\src\Nvidea.Windows\Nvidea.Windows.csproj -c Release
```

The default model router is NVIDIA Nemotron Nano for fast requests, Super for normal requests, and Ultra for deep reasoning. They can be changed before launch with `NVIDEA_MODEL_FAST`, `NVIDEA_MODEL_STANDARD`, and `NVIDEA_MODEL_DEEP`; the configured model must remain available from Nebius Token Factory.

Use `Ctrl+Shift+Space` to bring NVIDEA forward. Text is always reviewable before it is sent. `research:` starts the staged Nemotron + Tavily workflow. Consequential browser actions require their own approval.

## Optional local capabilities

- Voice is Windows-local through the installed speech recognizer. It requires a microphone, a Windows desktop speech recognizer, and explicit per-capture consent. It never sends audio to Nebius.
- Local memory embeddings are optional and use Ollama only at the loopback `/api/embed` endpoint. Install Ollama, pull an embedding model, then set `NVIDEA_LOCAL_EMBEDDINGS=true`. This changes memory retrieval only; it does not replace NVIDIA/Nebius reasoning.

## Remote Serverless research

Remote research and the signed-worker canary are intentionally disabled unless the full `NVIDEA_LIVE_*` deployment configuration is supplied. They can create paid Nebius work, and the UI asks for explicit confirmation before a canary or dispatch.

The worker signing private key is never configured on Windows. Store it in a version-pinned Nebius SecretStash/MysteryBox secret and configure the matching Windows public pin. Follow [`nebius-research-worker.md`](nebius-research-worker.md) and [`worker-result-signing-key-lifecycle.md`](worker-result-signing-key-lifecycle.md).

## Build a release artifact

```powershell
.\scripts\publish-windows.ps1 -OutputDirectory .\artifacts\NVIDEA-win-x64
```

The script creates a self-contained Windows build. For a GitHub Release, upload that generated output only after testing it on a clean Windows machine; source-build and release-binary validation are distinct evidence tiers.
