namespace IvTem.ExternalProcessManager;

/// <summary>
/// Describes the current diagnostic state of a configured external process.
/// </summary>
public enum ExternalProcessStatus
{
    NotStarted,
    Starting,
    Running,
    Stopping,
    RestartPending,
    Stopped,
    Faulted,
    InvalidConfiguration,
}
