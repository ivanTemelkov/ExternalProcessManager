namespace IvTem.ExternalProcessManager.Lifecycle;

internal enum ProcessCleanupOutcome
{
    AlreadyExited,
    GracefulStop,
    ForceKilled,
}
