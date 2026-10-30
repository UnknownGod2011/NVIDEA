# Local Windows voice invocation

keyboard-wtf supports a local, review-first voice entry path on Windows.

## Usage

- Open **Settings** to configure global shortcuts. Defaults are `Ctrl+Shift+Space` for text invocation, `Ctrl+Alt+D` for Local Dictation, `Ctrl+Alt+K` for Smart Dictation, `Ctrl+Alt+Q` for Jarvis, `Ctrl+Alt+X` for Emergency Stop, and `Ctrl+Alt+,` to open Settings.
- The voice-mode shortcuts open keyboard-wtf and start that specific mode after an explicit microphone disclosure. The Voice button uses the selected mode in the main window.
- When Local Dictation or Smart Dictation is started from inside the app without a captured external target, the transcript or polished text stays in the prompt for review. It is not treated as a failed insertion.
- **Emergency stop** cancels an in-progress recognition operation.

Local Dictation and Smart Dictation are one-shot. keyboard-wtf never auto-runs the recognized text: without a captured external target, the transcript is placed in the prompt box for review and still requires the normal **Run** action. Jarvis is a bounded turn-taking conversation: local recognition ends each user turn after a pause, the transcript is sent to Nemotron on Nebius, and the response is spoken locally with Windows SAPI. It is not full-duplex streaming speech-to-speech.

## Privacy boundary

The hackathon desktop uses the Windows desktop speech-recognition stack (`System.Speech` / SAPI) with an installed local recognizer. Captured audio is not intentionally routed to Nebius, Tavily, Token Factory, or another cloud speech API.

Before each capture keyboard-wtf discloses that the default microphone will be used and asks for explicit confirmation. Declining does not open the microphone.

The global voice shortcuts mirror the normal invocation privacy model: the foreground application and insertion target are captured before keyboard-wtf activates. Clipboard text is only captured when the existing **Allow clipboard context** control is enabled. Recognition itself does not send that context anywhere. Shortcut choices are saved per user under `%LOCALAPPDATA%\keyboard-wtf\keyboard-settings.json`; the file contains shortcut names only, never credentials.

## Availability

The voice entry point requires:

1. Windows microphone access for the desktop application.
2. At least one installed Windows desktop speech recognizer/language.
3. A working default recording device.

If no compatible recognizer is installed, keyboard-wtf leaves text invocation fully available and reports the voice path as locally unavailable.

## Failure and cancellation semantics

- Recognition is bounded to 20 seconds per attempt.
- Application shutdown and **Emergency stop** cancel the current recognition operation.
- Empty, invalid-confidence, or oversized transcripts fail closed instead of being silently executed or truncated.
- Low-confidence results are still reviewable, but the UI explicitly marks them as low-confidence.
- Speech/runtime failures are shown as generic local remediation guidance rather than raw provider or device exceptions.

## Dependency choice

`System.Speech` is Windows-only and wraps the Microsoft Speech API. The Windows project pins a stable package version that supports the repository's `net8.0-windows` target. This preserves a genuinely local speech path without introducing a mandatory cloud speech provider into the NVIDIA/Nebius hackathon architecture.
