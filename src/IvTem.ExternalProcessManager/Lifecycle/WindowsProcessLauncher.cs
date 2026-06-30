using System.Diagnostics;
using IvTem.ExternalProcessManager.Configuration;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal sealed class WindowsProcessLauncher : IProcessLauncher
{
    public IProcessHandle Launch(EffectiveExternalProcessConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        TaskCompletionSource<ProcessExitResult> exitCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Process process = new()
        {
            StartInfo = WindowsProcessStartInfoFactory.Create(configuration),
            EnableRaisingEvents = true,
        };

        EventHandler exitHandler = (_, _) => CompleteExit(process, exitCompletion);
        process.Exited += exitHandler;

        try
        {
            if (process.Start() == false)
                throw new InvalidOperationException($"Process '{configuration.FileName}' did not start.");

            DateTimeOffset startedAt = new(process.StartTime);

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
            process.Exited -= exitHandler;
            process.Dispose();
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
}
