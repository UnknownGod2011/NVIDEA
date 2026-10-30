# keyboard-wtf judge setup: NVIDIA/Nebius Personal AI on Windows

keyboard-wtf is a Windows-first .NET 8 app. It uses NVIDIA Nemotron through Nebius Token Factory for cloud reasoning and Tavily for optional web research.

## Standard desktop path

Install the .NET 8 SDK, clone the repository, then open PowerShell in the repository root. No API key is needed to launch the app or try Local Dictation:

```powershell
dotnet restore .\src\Nvidea.Windows\Nvidea.Windows.csproj
dotnet run --project .\src\Nvidea.Windows\Nvidea.Windows.csproj -c Release
```

For cloud reasoning, create a Nebius Token Factory API key, open **Settings → Models & keys**, paste it into the Nebius field, and save. Add a Tavily key there only if you want web research. The app protects saved keys with Windows CurrentUser DPAPI. Restart keyboard-wtf after changing provider keys or model routing. You can also configure `NEBIUS_API_KEY` and `TAVILY_API_KEY` in the environment; environment variables take precedence over saved values.

The default model router is NVIDIA Nemotron Nano for fast requests, Super for normal requests, and Ultra for deep reasoning. Choose model identifiers in **Settings → Models & keys**, or set `NVIDEA_MODEL_FAST`, `NVIDEA_MODEL_STANDARD`, and `NVIDEA_MODEL_DEEP` before launch. Configured models must be available from Nebius Token Factory.

Use `Ctrl+Shift+Space` to bring keyboard-wtf forward. Text is always reviewable before it is sent. `research:` starts the staged Nemotron + Tavily workflow. Consequential browser actions require their own approval. Configure the app, Local Dictation, Smart Dictation, Jarvis, emergency-stop and Settings hotkeys in **Settings → Shortcuts**. **Settings → Voice & Assistant** lets you choose an installed Windows recognizer language, adjust listening and pause timing, personalize Jarvis's name and style, select a local SAPI voice and rate, disable spoken output, and turn personal-memory retrieval on or off. These voice preferences apply after restarting the app; shortcut changes apply immediately.

## Optional local capabilities

- Voice input is Windows-local through the selected installed speech recognizer. It requires a microphone, a Windows desktop speech recognizer, and explicit per-capture consent. It never sends audio to Nebius. Jarvis replies use Nemotron text streaming and local SAPI speech, so speech can begin before the full response has finished generating; this remains turn-taking rather than full-duplex audio.
- Local memory embeddings are optional and use Ollama only at the loopback `/api/embed` endpoint. Install Ollama, pull an embedding model, then set `NVIDEA_LOCAL_EMBEDDINGS=true`. This changes memory retrieval only; it does not replace NVIDIA/Nebius reasoning.

## Remote Serverless research

Remote research and the signed-worker canary are intentionally disabled unless the full `NVIDEA_LIVE_*` deployment configuration is supplied. They can create paid Nebius work, and the UI asks for explicit confirmation before a canary or dispatch.

The worker signing private key is never configured on Windows. Store it in a version-pinned Nebius SecretStash/MysteryBox secret and configure the matching Windows public pin. Follow [`nebius-research-worker.md`](nebius-research-worker.md) and [`worker-result-signing-key-lifecycle.md`](worker-result-signing-key-lifecycle.md).

## Build a release artifact

```powershell
.\scripts\publish-windows.ps1 -OutputDirectory .\artifacts\NVIDEA-win-x64
```

The script creates a self-contained Windows build. For a GitHub Release, upload that generated output only after testing it on a clean Windows machine; source-build and release-binary validation are distinct evidence tiers.
