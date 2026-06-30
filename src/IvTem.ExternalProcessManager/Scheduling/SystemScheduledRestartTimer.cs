namespace IvTem.ExternalProcessManager.Scheduling;

internal sealed class SystemScheduledRestartTimer : IScheduledRestartTimer
{
    public SystemScheduledRestartTimer(Func<Task> callback, ILocalClock clock)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentNullException.ThrowIfNull(clock);

        Callback = callback;
        Clock = clock;
        Timer = new Timer(OnElapsed);
    }

    private Func<Task> Callback { get; }

    private ILocalClock Clock { get; }

    private Timer Timer { get; }

    private Lock StateLock { get; } = new();

    private bool IsDisposed { get; set; }

    public void Schedule(DateTimeOffset dueTime)
    {
        lock (StateLock)
        {
            if (IsDisposed)
                return;

            TimeSpan dueDelay = dueTime - Clock.Now;

            if (dueDelay < TimeSpan.Zero)
                dueDelay = TimeSpan.Zero;

            Timer.Change(dueDelay, Timeout.InfiniteTimeSpan);
        }
    }

    public void Cancel()
    {
        lock (StateLock)
        {
            if (IsDisposed)
                return;

            Timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }

    public void Dispose()
    {
        lock (StateLock)
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            Timer.Dispose();
        }
    }

    private void OnElapsed(object? state)
        => _ = InvokeCallback();

    private async Task InvokeCallback()
    {
        try
        {
            await Callback()
                .ConfigureAwait(continueOnCapturedContext: false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
