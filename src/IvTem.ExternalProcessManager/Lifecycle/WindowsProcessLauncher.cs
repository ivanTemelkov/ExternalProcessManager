using System.Diagnostics;
using IvTem.ExternalProcessManager.Configuration;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal sealed class WindowsProcessLauncher : IProcessLauncher
{
    public IProcessHandle Launch(EffectiveExternalProcessConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        WindowsPlatform.ThrowIfUnsupported();

        TaskCompletionSource<ProcessExitResult> exitCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessStartInfo startInfo = WindowsProcessStartInfoFactory.Create(configuration);
        Process? process = null;

        EventHandler? exitHandler = null;

        try
        {
            process = WindowsProcessCreation.Start(startInfo);
            process.EnableRaisingEvents = true;
            exitHandler = (_, _) => CompleteExit(process, exitCompletion);
            process.Exited += exitHandler;

            DateTimeOffset startedAt = GetStartTime(process);

            if (process.HasExited)
                CompleteExit(process, exitCompletion);

            return new WindowsProcessHandle(
                process,
                exitHandler,
                process.Id,
                startedAt,
                exitCompletion);
        }
        catch
        {
            if (process is not null && exitHandler is not null)
                process.Exited -= exitHandler;

            process?.Dispose();
            throw;
        }
    }

    private static void CompleteExit(
        Process process,
        TaskCompletionSource<ProcessExitResult> exitCompletion)
    {
        int? exitCode = TryGetExitCode(process);

        exitCompletion.TrySetResult(new ProcessExitResult
        {
            ExitCode = exitCode,
            ExitedAt = DateTimeOffset.Now,
        });
    }

    private static int? TryGetExitCode(Process process)
    {
        try
        {
            return process.ExitCode;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static DateTimeOffset GetStartTime(Process process)
    {
        try
        {
            return new DateTimeOffset(process.StartTime);
        }
        catch (InvalidOperationException)
        {
            return DateTimeOffset.Now;
        }
    }
}
