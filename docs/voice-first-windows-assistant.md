# keyboard-wtf voice-first Windows assistant

## Permanent product goal

keyboard-wtf is a Windows Personal AI with three first-class interaction modes:

1. **Local Dictation** — private local speech-to-text and safe exact-text insertion.
2. **Smart Dictation** — local speech-to-text, NVIDIA Nemotron grammar/formatting cleanup, reviewed safe insertion.
3. **Jarvis** — a spoken multi-turn assistant using NVIDIA Nemotron through Nebius, local speech output, memory-aware replies, and permissioned typed Windows tools.

These are product capabilities, not decorative mode labels. A mode may be described as available only when its matching runtime is configured and the Windows host can initialize its local speech prerequisites.

## Reference-to-implementation map

The read-only `keyboard.wtf` reference established the interaction pattern. This implementation adapts it around NVIDIA Nemotron and Nebius:

| Reference pattern | keyboard-wtf implementation |
| --- | --- |
| microphone lifecycle, partial transcript, pause detection | `SystemSpeechStreamingRecognizer` with local continuous Windows recognition and a 900 ms turn boundary |
| dictation into previous app | `WindowsFocusTarget` + `WindowsTextInsertion`; a target mismatch aborts insertion |
| assistant conversation | `JarvisConversation`, backed by keyboard-wtf's `IAgentInferenceClient`, Nebius Token Factory and privacy-aware memory |
| local spoken response and interruption | `LocalSpeechOutput` using Windows SAPI and cancellable playback |
| tool calling | bounded JSON-schema tool definitions, `IJarvisToolExecutor`, visible approvals, no shell capability |
| assistant safety / browser execution | existing keyboard-wtf permissions, audit, emergency-stop and verified Playwright browser subsystems remain the authority |

## Runtime behavior

### Local Dictation

The configurable Local Dictation shortcut captures the foreground target before the desktop window activates. Its default is `Ctrl+Alt+D`. Continuous local recognition displays partial text and finishes after a pause. The final transcript is bounded, focus is restored to the captured window, and insertion occurs through a temporary Unicode clipboard paste. If Windows cannot restore the exact target, nothing is typed.

### Smart Dictation

The same local recognition path produces the transcript. Only that text is sent to the existing Nebius Token Factory/Nemotron client with a meaning-preservation instruction. The result is shown for review and requires a visible insertion approval. Raw microphone audio is never sent to Nebius.

### Jarvis

Jarvis maintains a bounded in-memory conversation window and retrieves eligible Personal Memory on each turn. It supports model-selected typed calls only for the small approved Windows tool set. Every actual side effect has a local visible approval; unsupported tools, malformed arguments and all shell-like requests are rejected. After a response, local SAPI speaks it. The configurable Jarvis shortcut (default `Ctrl+Alt+Q`) during speech cancels playback and returns Jarvis to listening; Emergency Stop (default `Ctrl+Alt+X`) cancels speech and the active session.

## Speech provider strategy

The default requires no separate speech-provider account:

- Windows desktop speech recognizer for local ASR.
- Windows SAPI for local TTS.
- Nebius Token Factory only for Smart Dictation/Jarvis reasoning.

NVIDIA Riva/Speech NIM can provide streaming ASR/TTS when an operator deploys an endpoint and configures its separate authentication. It is not implied by `NEBIUS_API_KEY`, and no unconfigured cloud speech endpoint is contacted. A future adapter must preserve the same local transcript/privacy, cancellation, bounded-buffer and provider-free test contracts.

## Current qualification boundary

Provider-free tests cover conversational history, typed-tool receipt handling and rejection of unsupported tools. The WPF project compiles. Real microphone/speaker qualification still needs an operator Windows session with a microphone, speaker, desktop speech language, and (for Smart Dictation/Jarvis reasoning) a valid Nebius key. Do not represent mocked tests or a WPF build as physical-device proof.
