using Microsoft.Extensions.Logging;

namespace IvTem.ExternalProcessManager.Scheduling;

internal sealed partial class SystemScheduledRestartTimer : IScheduledRestartTimer
{
    public SystemScheduledRestartTimer(
        Func<Task> callback,
        ILocalClock clock,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        Callback = callback;
        Clock = clock;
        Logger = logger;
        Timer = new Timer(OnElapsed);
    }

    private Func<Task> Callback { get; }

    private ILocalClock Clock { get; }

    private ILogger Logger { get; }

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
            // Timer callbacks are canceled during lifecycle shutdown.
        }
        catch (ObjectDisposedException)
        {
            // Timer callbacks can race with timer disposal.
        }
        catch (Exception exception)
        {
            LogScheduledRestartTimerCallbackFailed(Logger, exception);
        }
    }

    [LoggerMessage(EventId = 4000, Level = LogLevel.Error, Message = "Scheduled restart timer callback failed unexpectedly.")]
    private static partial void LogScheduledRestartTimerCallbackFailed(ILogger logger, Exception exception);
}
