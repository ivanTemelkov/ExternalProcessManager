namespace IvTem.ExternalProcessManager.Lifecycle;

internal interface IProcessHandle : IDisposable
{
    int ProcessId { get; }

    DateTimeOffset StartedAt { get; }

    Task<ProcessExitResult> Exited { get; }
}
