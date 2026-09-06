namespace Nvidea.Core.Desktop;

public enum DesktopAgentState
{
    Idle,
    Listening,
    Thinking,
    Researching,
    Acting,
    WaitingForApproval,
    Completed,
    Cancelled,
    Failed
}

public sealed record DesktopAgentStatus(
    DesktopAgentState State,
    string? Detail = null,
    DateTimeOffset? ChangedAt = null);

/// <summary>
/// Small state/cancellation coordinator for the Windows orb/hotkey shell. It owns no
/// provider credentials and exposes an emergency stop that cancels the active invocation.
/// </summary>
public sealed class DesktopSessionController : IDisposable
{
    private readonly DesktopInvocationService _desktop;
    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private CancellationTokenSource? _active;
    private DesktopAgentStatus _status;
    private bool _disposed;

    public DesktopSessionController(DesktopInvocationService desktop, TimeProvider? timeProvider = null)
    {
        _desktop = desktop ?? throw new ArgumentNullException(nameof(desktop));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _status = NewStatus(DesktopAgentState.Idle);
    }

    public event EventHandler<DesktopAgentStatus>? StatusChanged;

    public DesktopAgentStatus Status
    {
        get
        {
            lock (_gate)
                return _status;
        }
    }

    public async Task<DesktopInvocationResult> InvokeAsync(
        DesktopInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        CancellationTokenSource active;
        lock (_gate)
        {
            if (_active is not null)
                throw new InvalidOperationException("A desktop invocation is already running.");

            active = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _active = active;
        }

        SetStatus(request.Mode == DesktopInvocationMode.Research
            ? DesktopAgentState.Researching
            : DesktopAgentState.Thinking,
            "Processing with NVIDEA");

        try
        {
            var result = await _desktop.InvokeAsync(request, active.Token).ConfigureAwait(false);
            SetStatus(DesktopAgentState.Completed, "Response ready");
            return result;
        }
        catch (OperationCanceledException) when (active.IsCancellationRequested)
        {
            SetStatus(DesktopAgentState.Cancelled, "Stopped by user");
            throw;
        }
        catch
        {
            SetStatus(DesktopAgentState.Failed, "Invocation failed");
            throw;
        }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_active, active))
                    _active = null;
            }
            active.Dispose();
        }
    }

    public bool EmergencyStop()
    {
        ThrowIfDisposed();
        CancellationTokenSource? active;
        lock (_gate)
            active = _active;

        if (active is null)
            return false;

        active.Cancel();
        return true;
    }

    public void ResetToIdle()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            if (_active is not null)
                throw new InvalidOperationException("Cannot reset while an invocation is running.");
        }
        SetStatus(DesktopAgentState.Idle);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        lock (_gate)
        {
            _active?.Cancel();
            _active?.Dispose();
            _active = null;
        }
    }

    private void SetStatus(DesktopAgentState state, string? detail = null)
    {
        var status = NewStatus(state, detail);
        lock (_gate)
            _status = status;
        StatusChanged?.Invoke(this, status);
    }

    private DesktopAgentStatus NewStatus(DesktopAgentState state, string? detail = null) =>
        new(state, detail, _timeProvider.GetUtcNow());

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
