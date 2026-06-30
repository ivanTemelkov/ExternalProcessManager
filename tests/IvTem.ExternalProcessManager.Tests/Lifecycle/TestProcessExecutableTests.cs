using System.Diagnostics;
using System.Globalization;
using IvTem.ExternalProcessManager.Configuration;
using IvTem.ExternalProcessManager.Lifecycle;
using IvTem.ExternalProcessManager.Tests;

namespace IvTem.ExternalProcessManager.Tests.Lifecycle;

public sealed class TestProcessExecutableTests
{
    [Fact]
    public async Task ImmediateExitReturnsRequestedExitCode()
    {
        if (OperatingSystem.IsWindows() == false)
            return;

        using Process process = LaunchDirect("exit", "--exit-code", "17");

        int exitCode = await WaitForExit(process);

        Assert.Equal(17, exitCode);
    }

    [Fact]
    public async Task DelayedExitReturnsRequestedExitCode()
    {
        if (OperatingSystem.IsWindows() == false)
            return;

        using Process process = LaunchDirect("delay-exit", "--delay-ms", "50", "--exit-code", "23");

        int exitCode = await WaitForExit(process);

        Assert.Equal(23, exitCode);
    }

    [Fact]
    public async Task RunUntilKilledRemainsAliveUntilTerminated()
    {
        if (OperatingSystem.IsWindows() == false)
            return;

        using TemporaryFile readyFile = TemporaryFile.Create();
        using Process process = LaunchDirect("run-until-killed", "--ready-file", readyFile.Path);

        await readyFile.WaitUntilExists();

        Assert.False(process.HasExited);

        process.Kill(entireProcessTree: true);
        await WaitForExit(process);
    }

    [Fact]
    public async Task ChildProcessModeCreatesDescendantProcess()
    {
        if (OperatingSystem.IsWindows() == false)
            return;

        using TemporaryFile readyFile = TemporaryFile.Create();
        using TemporaryFile childPidFile = TemporaryFile.Create();
        using Process process = LaunchDirect(
            "spawn-child",
            "--ready-file",
            readyFile.Path,
            "--child-pid-file",
            childPidFile.Path);

        try
        {
            await readyFile.WaitUntilExists();
            int childProcessId = await ReadProcessId(childPidFile.Path);

            using Process childProcess = Process.GetProcessById(childProcessId);

            Assert.False(process.HasExited);
            Assert.False(childProcess.HasExited);

            process.Kill(entireProcessTree: true);
            await WaitForExit(process);
            await AssertProcessExited(childProcessId);
        }
        finally
        {
            KillProcessTree(process);
        }
    }

    [Fact]
    public async Task CtrlBreakHandlingModeExitsCleanly()
    {
        if (OperatingSystem.IsWindows() == false)
            return;

        using TemporaryFile readyFile = TemporaryFile.Create();
        using TemporaryFile stoppedFile = TemporaryFile.Create();
        using IProcessHandle handle = LaunchManagedHelper(
            "handle-ctrl-break",
            "--ready-file",
            readyFile.Path,
            "--stopped-file",
            stoppedFile.Path);
        await readyFile.WaitUntilExists();
        WindowsProcessCleanup cleanup = new(new TestLogger<WindowsProcessCleanup>());

        ProcessCleanupResult result = await cleanup.Stop(
            handle,
            TimeSpan.FromSeconds(5),
            CancellationToken.None);

        Assert.Equal(ProcessCleanupOutcome.GracefulStop, result.Outcome);
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(stoppedFile.Path));
    }

    [Fact]
    public async Task IgnoreModeRequiresForcedTermination()
    {
        if (OperatingSystem.IsWindows() == false)
            return;

        using TemporaryFile readyFile = TemporaryFile.Create();
        using IProcessHandle handle = LaunchManagedHelper(
            "ignore-ctrl-break",
            "--ready-file",
            readyFile.Path);
        await readyFile.WaitUntilExists();
        WindowsProcessCleanup cleanup = new(new TestLogger<WindowsProcessCleanup>());

        ProcessCleanupResult result = await cleanup.Stop(
            handle,
            TimeSpan.FromMilliseconds(250),
            CancellationToken.None);

        Assert.Equal(ProcessCleanupOutcome.ForceKilled, result.Outcome);
        Assert.NotEqual(0, result.ExitCode);
    }

    private static Process LaunchDirect(string mode, params string[] arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = TestProcessPath.Resolve(),
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add(mode);

        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("The test helper executable did not start.");
    }

    private static IProcessHandle LaunchManagedHelper(string mode, params string[] arguments)
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

    private static async Task<int> WaitForExit(Process process)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));

        await process.WaitForExitAsync(timeout.Token)
            .ConfigureAwait(continueOnCapturedContext: false);

        return process.ExitCode;
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

    private static void KillProcessTree(Process process)
    {
        try
        {
            if (process.HasExited == false)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The process exited while the test was cleaning up.
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
