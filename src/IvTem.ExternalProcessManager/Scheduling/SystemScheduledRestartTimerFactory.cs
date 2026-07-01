using Microsoft.Extensions.Logging;

namespace IvTem.ExternalProcessManager.Scheduling;

internal sealed class SystemScheduledRestartTimerFactory : IScheduledRestartTimerFactory
{
    public SystemScheduledRestartTimerFactory(
        ILocalClock clock,
        ILogger<SystemScheduledRestartTimerFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        Clock = clock;
        Logger = logger;
    }

    private ILocalClock Clock { get; }

    private ILogger<SystemScheduledRestartTimerFactory> Logger { get; }

    public IScheduledRestartTimer Create(Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return new SystemScheduledRestartTimer(callback, Clock, Logger);
    }
}
