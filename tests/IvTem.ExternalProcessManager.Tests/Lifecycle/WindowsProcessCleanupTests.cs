using System.Diagnostics;
using System.Globalization;
using IvTem.ExternalProcessManager.Configuration;
using IvTem.ExternalProcessManager.Lifecycle;

namespace IvTem.ExternalProcessManager.Tests.Lifecycle;

public sealed class WindowsProcessCleanupTests
{
    [Fact]
    public async Task StopSendsCtrlBreakBeforeTimeout()
    {
        if (OperatingSystem.IsWindows() == false)
            return;

        using TemporaryFile readyFile = TemporaryFile.Create();
        using TemporaryFile stoppedFile = TemporaryFile.Create();
        using IProcessHandle handle = LaunchHelper(
            "handle-ctrl-break",
            "--ready-file",
            readyFile.Path,
            "--stopped-file",
            stoppedFile.Path);
        await readyFile.WaitUntilExists();

        WindowsProcessCleanup cleanup = new();

        ProcessCleanupResult result = await cleanup.Stop(
            handle,
            TimeSpan.FromSeconds(5),
            CancellationToken.None);

        Assert.Equal(ProcessCleanupOutcome.GracefulStop, result.Outcome);
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(stoppedFile.Path));
    }

    [Fact]
    public async Task StopForceKillsProcessAfterTimeout()
    {
        if (OperatingSystem.IsWindows() == false)
            return;

        using TemporaryFile readyFile = TemporaryFile.Create();
        using IProcessHandle handle = LaunchHelper(
            "ignore-ctrl-break",
            "--ready-file",
            readyFile.Path);
        await readyFile.WaitUntilExists();

        WindowsProcessCleanup cleanup = new();

        ProcessCleanupResult result = await cleanup.Stop(
            handle,
            TimeSpan.FromMilliseconds(250),
            CancellationToken.None);

        Assert.Equal(ProcessCleanupOutcome.ForceKilled, result.Outcome);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task StopForceKillsChildProcessTree()
    {
        if (OperatingSystem.IsWindows() == false)
            return;

        using TemporaryFile readyFile = TemporaryFile.Create();
        using TemporaryFile childPidFile = TemporaryFile.Create();
        using IProcessHandle handle = LaunchHelper(
            "spawn-child-ignore-ctrl-break",
            "--ready-file",
            readyFile.Path,
            "--child-pid-file",
            childPidFile.Path);
        await readyFile.WaitUntilExists();
        int childProcessId = await ReadProcessId(childPidFile.Path);

        WindowsProcessCleanup cleanup = new();

        ProcessCleanupResult result = await cleanup.Stop(
            handle,
            TimeSpan.FromMilliseconds(250),
            CancellationToken.None);

        Assert.Equal(ProcessCleanupOutcome.ForceKilled, result.Outcome);
        await AssertProcessExited(childProcessId);
    }

    private static IProcessHandle LaunchHelper(string mode, params string[] arguments)
    {
        WindowsProcessLauncher launcher = new();
        return launcher.Launch(new EffectiveExternalProcessConfiguration
        {
            Path = "ExternalProcessManager:Processes:0",
            Alias = "worker",
            AliasKey = "worker",
            FileName = TestProcessPath.Resolve(),
            ArgumentMode = EffectiveProcessArgumentMode.ArgumentList,
            ArgumentList = [mode, .. arguments],
            Restart = new EffectiveRestartConfiguration
            {
                Mode = ExternalProcessRestartMode.NonZeroExitCode,
                MinBackoff = TimeSpan.FromSeconds(2),
                MaxBackoff = TimeSpan.FromMinutes(1),
                StableRunDuration = TimeSpan.FromMinutes(5),
                GracefulStopTimeout = TimeSpan.FromSeconds(10),
            },
        });
    }

    private static async Task<int> ReadProcessId(string path)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));

        while (timeout.IsCancellationRequested == false)
        {
            if (File.Exists(path))
            {
                string text = await File.ReadAllTextAsync(path, timeout.Token)
                    .ConfigureAwait(continueOnCapturedContext: false);
                return int.Parse(text, CultureInfo.InvariantCulture);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(25), timeout.Token)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        throw new TimeoutException("The helper did not write a child process ID.");
    }

    private static async Task AssertProcessExited(int processId)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));

        try
        {
            using Process process = Process.GetProcessById(processId);
            await process.WaitForExitAsync(timeout.Token)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
        catch (ArgumentException)
        {
            // The process already exited and Windows has released its process ID.
        }
    }

    private sealed class TemporaryFile : IDisposable
    {
        private TemporaryFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryFile Create()
            => new(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.CreateVersion7():N}.tmp"));

        public async Task WaitUntilExists()
        {
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));

            while (timeout.IsCancellationRequested == false)
            {
                if (File.Exists(Path))
                    return;

                await Task.Delay(TimeSpan.FromMilliseconds(25), timeout.Token)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }

            throw new TimeoutException($"Temporary file '{Path}' was not created.");
        }

        public void Dispose()
        {
            try
            {
                File.Delete(Path);
            }
            catch (IOException)
            {
                // Temporary files are best-effort cleanup in tests.
            }
            catch (UnauthorizedAccessException)
            {
                // Temporary files are best-effort cleanup in tests.
            }
        }
    }
}
