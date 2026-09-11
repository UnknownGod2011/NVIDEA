# Local Windows voice invocation

NVIDEA supports a local, review-first voice entry path on Windows.

## Usage

- `Ctrl+Shift+Space` opens the normal text invocation surface.
- `Ctrl+Shift+V` opens NVIDEA and starts the voice flow after an explicit microphone disclosure.
- The **Voice** button provides the same flow from the desktop window.
- **Emergency stop** cancels an in-progress recognition operation.

Voice recognition is one-shot. NVIDEA never auto-runs the recognized text: the transcript is placed in the prompt box for review and still requires the normal **Run** action.

## Privacy boundary

The hackathon desktop uses the Windows desktop speech-recognition stack (`System.Speech` / SAPI) with an installed local recognizer. Captured audio is not intentionally routed to Nebius, Tavily, Token Factory, or another cloud speech API.

Before each capture NVIDEA discloses that the default microphone will be used and asks for explicit confirmation. Declining does not open the microphone.

The global voice hotkey mirrors the normal global invocation privacy model: foreground application context is captured before NVIDEA activates. Clipboard text is only captured when the existing **Allow clipboard context** control is enabled. Recognition itself does not send that context anywhere.

## Availability

The voice entry point requires:

1. Windows microphone access for the desktop application.
2. At least one installed Windows desktop speech recognizer/language.
3. A working default recording device.

If no compatible recognizer is installed, NVIDEA leaves text invocation fully available and reports the voice path as locally unavailable.

## Failure and cancellation semantics

- Recognition is bounded to 20 seconds per attempt.
- Application shutdown and **Emergency stop** cancel the current recognition operation.
- Empty, invalid-confidence, or oversized transcripts fail closed instead of being silently executed or truncated.
- Low-confidence results are still reviewable, but the UI explicitly marks them as low-confidence.
- Speech/runtime failures are shown as generic local remediation guidance rather than raw provider or device exceptions.

## Dependency choice

`System.Speech` is Windows-only and wraps the Microsoft Speech API. The Windows project pins a stable package version that supports the repository's `net8.0-windows` target. This preserves a genuinely local speech path without introducing a mandatory cloud speech provider into the NVIDIA/Nebius hackathon architecture.
