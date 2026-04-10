namespace Domain;

/// <summary>Broadcast wake for <see cref="IDomainHostsChangedSignal"/> using a rotating <see cref="TaskCompletionSource{T}"/>.</summary>
public sealed class DomainHostsChangedSignal : IDomainHostsChangedSignal
{
    private readonly object _lock = new();
    private TaskCompletionSource<bool> _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void NotifyHostsChanged()
    {
        TaskCompletionSource<bool> previous;
        lock (_lock)
        {
            previous = _pending;
            _pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        previous.TrySetResult(true);
    }

    public Task WhenHostsChangedAsync(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<bool> tcs;
        lock (_lock)
        {
            tcs = _pending;
        }

        return tcs.Task.WaitAsync(cancellationToken);
    }
}
