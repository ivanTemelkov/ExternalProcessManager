using System.Diagnostics;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal sealed class WindowsProcessCleanup : IProcessCleanup
{
    public async Task<ProcessCleanupResult> Stop(
        IProcessHandle handle,
        TimeSpan gracefulStopTimeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handle);

        if (handle.Exited.IsCompleted)
            return await CreateResult(handle, ProcessCleanupOutcome.AlreadyExited, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

        WindowsConsoleControl.TrySendCtrlBreakToProcessGroup(handle.ProcessId);

        if (await WaitForExit(handle, gracefulStopTimeout, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false))
        {
            return await CreateResult(handle, ProcessCleanupOutcome.GracefulStop, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        KillProcessTree(handle.ProcessId);

        await handle.Exited.WaitAsync(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        return await CreateResult(handle, ProcessCleanupOutcome.ForceKilled, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    private static async Task<bool> WaitForExit(
        IProcessHandle handle,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        try
        {
            await handle.Exited.WaitAsync(timeout, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private static void KillProcessTree(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);

            if (process.HasExited)
                return;

            process.Kill(entireProcessTree: true);
        }
        catch (ArgumentException)
        {
            // The process exited before the force-kill fallback opened it.
        }
        catch (InvalidOperationException)
        {
            // The process exited while the force-kill fallback was checking it.
        }
    }

    private static async Task<ProcessCleanupResult> CreateResult(
        IProcessHandle handle,
        ProcessCleanupOutcome outcome,
        CancellationToken cancellationToken)
    {
        ProcessExitResult exit = await handle.Exited.WaitAsync(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        return new ProcessCleanupResult
        {
            ProcessId = handle.ProcessId,
            Outcome = outcome,
            ExitCode = exit.ExitCode,
            CompletedAt = DateTimeOffset.Now,
        };
    }
}
