using IvTem.ExternalProcessManager.Configuration;
using IvTem.ExternalProcessManager.Scheduling;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal sealed class ExternalProcessSupervisorFactory : IExternalProcessSupervisorFactory
{
    public ExternalProcessSupervisorFactory(
        IProcessLauncher launcher,
        IProcessCleanup cleanup,
        IRestartDelay restartDelay,
        ILocalClock clock,
        IScheduledRestartTimerFactory scheduledRestartTimerFactory)
    {
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(cleanup);
        ArgumentNullException.ThrowIfNull(restartDelay);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(scheduledRestartTimerFactory);

        Launcher = launcher;
        Cleanup = cleanup;
        RestartDelay = restartDelay;
        Clock = clock;
        ScheduledRestartTimerFactory = scheduledRestartTimerFactory;
    }

    private IProcessLauncher Launcher { get; }

    private IProcessCleanup Cleanup { get; }

    private IRestartDelay RestartDelay { get; }

    private ILocalClock Clock { get; }

    private IScheduledRestartTimerFactory ScheduledRestartTimerFactory { get; }

    public IExternalProcessSupervisor Create(EffectiveExternalProcessConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new ExternalProcessSupervisor(
            configuration,
            Launcher,
            Cleanup,
            RestartDelay,
            Clock,
            ScheduledRestartTimerFactory);
    }
}
