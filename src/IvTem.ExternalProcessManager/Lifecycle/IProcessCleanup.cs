namespace IvTem.ExternalProcessManager.Lifecycle;

internal interface IProcessCleanup
{
    Task<ProcessCleanupResult> Stop(
        IProcessHandle handle,
        TimeSpan gracefulStopTimeout,
        CancellationToken cancellationToken);
}
