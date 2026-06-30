namespace IvTem.ExternalProcessManager.Scheduling;

internal sealed class SystemScheduledRestartTimerFactory : IScheduledRestartTimerFactory
{
    public SystemScheduledRestartTimerFactory(ILocalClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        Clock = clock;
    }

    private ILocalClock Clock { get; }

    public IScheduledRestartTimer Create(Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return new SystemScheduledRestartTimer(callback, Clock);
    }
}
