using IvTem.ExternalProcessManager.Configuration;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal sealed class ExternalProcessSupervisorFactory : IExternalProcessSupervisorFactory
{
    public ExternalProcessSupervisorFactory(
        IProcessLauncher launcher,
        IProcessCleanup cleanup,
        IRestartDelay restartDelay)
    {
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(cleanup);
        ArgumentNullException.ThrowIfNull(restartDelay);

        Launcher = launcher;
        Cleanup = cleanup;
        RestartDelay = restartDelay;
    }

    private IProcessLauncher Launcher { get; }

    private IProcessCleanup Cleanup { get; }

    private IRestartDelay RestartDelay { get; }

    public IExternalProcessSupervisor Create(EffectiveExternalProcessConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new ExternalProcessSupervisor(configuration, Launcher, Cleanup, RestartDelay);
    }
}
