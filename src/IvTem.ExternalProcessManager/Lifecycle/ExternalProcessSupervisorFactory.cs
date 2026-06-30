using IvTem.ExternalProcessManager.Configuration;
using IvTem.ExternalProcessManager.Scheduling;
using Microsoft.Extensions.Logging;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal sealed class ExternalProcessSupervisorFactory : IExternalProcessSupervisorFactory
{
    public ExternalProcessSupervisorFactory(
        IProcessLauncher launcher,
        IProcessCleanup cleanup,
        IRestartDelay restartDelay,
        ILocalClock clock,
        IScheduledRestartTimerFactory scheduledRestartTimerFactory,
        ILogger<ExternalProcessSupervisor> logger)
    {
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(cleanup);
        ArgumentNullException.ThrowIfNull(restartDelay);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(scheduledRestartTimerFactory);
        ArgumentNullException.ThrowIfNull(logger);

        Launcher = launcher;
        Cleanup = cleanup;
        RestartDelay = restartDelay;
        Clock = clock;
        ScheduledRestartTimerFactory = scheduledRestartTimerFactory;
        Logger = logger;
    }

    private IProcessLauncher Launcher { get; }

    private IProcessCleanup Cleanup { get; }

    private IRestartDelay RestartDelay { get; }

    private ILocalClock Clock { get; }

    private IScheduledRestartTimerFactory ScheduledRestartTimerFactory { get; }

    private ILogger<ExternalProcessSupervisor> Logger { get; }

    public IExternalProcessSupervisor Create(EffectiveExternalProcessConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new ExternalProcessSupervisor(
            configuration,
            Launcher,
            Cleanup,
            RestartDelay,
            Clock,
            ScheduledRestartTimerFactory,
            Logger);
    }
}
