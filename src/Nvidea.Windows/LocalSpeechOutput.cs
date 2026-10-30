using System.Speech.Synthesis;

namespace Nvidea.Windows;

/// <summary>Local SAPI speech output with cancellation. No response text is sent to a speech cloud.</summary>
internal sealed class LocalSpeechOutput : IDisposable
{
    private readonly SpeechSynthesizer _synthesizer = new();
    private readonly object _gate = new();
    private TaskCompletionSource? _active;
    private bool _disposed;

    public async Task SpeakAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        ThrowIfDisposed();
        TaskCompletionSource completion;
        lock (_gate)
        {
            _synthesizer.SpeakAsyncCancelAll();
            _active?.TrySetCanceled();
            completion = _active = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler<SpeakCompletedEventArgs>? handler = null;
            handler = (_, args) =>
            {
                _synthesizer.SpeakCompleted -= handler;
                if (args.Cancelled) completion.TrySetCanceled();
                else if (args.Error is not null) completion.TrySetException(args.Error);
                else completion.TrySetResult();
            };
            _synthesizer.SpeakCompleted += handler;
            _synthesizer.SpeakAsync(text);
        }
        using var registration = cancellationToken.Register(Stop);
        await completion.Task.ConfigureAwait(false);
    }

    public void Stop()
    {
        lock (_gate)
        {
            try { _synthesizer.SpeakAsyncCancelAll(); } catch (ObjectDisposedException) { }
            _active?.TrySetCanceled();
            _active = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _synthesizer.Dispose();
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
