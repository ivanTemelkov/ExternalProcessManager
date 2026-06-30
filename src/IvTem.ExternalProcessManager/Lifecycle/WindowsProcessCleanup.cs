using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal sealed partial class WindowsProcessCleanup : IProcessCleanup
{
    public WindowsProcessCleanup(ILogger<WindowsProcessCleanup> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        Logger = logger;
    }

    private ILogger<WindowsProcessCleanup> Logger { get; }

    public async Task<ProcessCleanupResult> Stop(
        IProcessHandle handle,
        TimeSpan gracefulStopTimeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handle);
        WindowsPlatform.ThrowIfUnsupported();

        if (handle.Exited.IsCompleted)
        {
            ProcessCleanupResult result = await CreateResult(handle, ProcessCleanupOutcome.AlreadyExited, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            LogCleanupCompleted(Logger, result.ProcessId, result.Outcome, result.ExitCode);
            return result;
        }

        LogGracefulStopStarted(Logger, handle.ProcessId, gracefulStopTimeout);
        WindowsConsoleControl.TrySendCtrlBreakToProcessGroup(handle.ProcessId);

        if (await WaitForExit(handle, gracefulStopTimeout, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false))
        {
            ProcessCleanupResult result = await CreateResult(handle, ProcessCleanupOutcome.GracefulStop, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            LogCleanupCompleted(Logger, result.ProcessId, result.Outcome, result.ExitCode);
            return result;
        }

        LogForceKillStarted(Logger, handle.ProcessId);
        KillProcessTree(handle.ProcessId);

        await handle.Exited.WaitAsync(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        ProcessCleanupResult forceKillResult = await CreateResult(handle, ProcessCleanupOutcome.ForceKilled, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
        LogCleanupCompleted(Logger, forceKillResult.ProcessId, forceKillResult.Outcome, forceKillResult.ExitCode);
        return forceKillResult;
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

    [LoggerMessage(EventId = 3000, Level = LogLevel.Information, Message = "Sending CTRL+BREAK to external process group {ProcessId}; graceful timeout is {GracefulStopTimeout}.")]
    private static partial void LogGracefulStopStarted(ILogger logger, int processId, TimeSpan gracefulStopTimeout);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning, Message = "Force-killing external process tree rooted at process ID {ProcessId}.")]
    private static partial void LogForceKillStarted(ILogger logger, int processId);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "External process cleanup completed for process ID {ProcessId} with outcome {Outcome} and exit code {ExitCode}.")]
    private static partial void LogCleanupCompleted(ILogger logger, int processId, ProcessCleanupOutcome outcome, int? exitCode);
}
