using System.Diagnostics;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal sealed class WindowsProcessHandle : IProcessHandle
{
    private Process Process { get; }

    private EventHandler ExitHandler { get; }

    private TaskCompletionSource<ProcessExitResult> ExitCompletion { get; }

    private bool IsDisposed { get; set; }

    public WindowsProcessHandle(
        Process process,
        EventHandler exitHandler,
        int processId,
        DateTimeOffset startedAt,
        TaskCompletionSource<ProcessExitResult> exitCompletion)
    {
        Process = process;
        ExitHandler = exitHandler;
        ProcessId = processId;
        StartedAt = startedAt;
        ExitCompletion = exitCompletion;
    }

    public int ProcessId { get; }

    public DateTimeOffset StartedAt { get; }

    public Task<ProcessExitResult> Exited => ExitCompletion.Task;

    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;
        Process.Exited -= ExitHandler;
        Process.Dispose();
        ExitCompletion.TrySetCanceled();
    }
}
