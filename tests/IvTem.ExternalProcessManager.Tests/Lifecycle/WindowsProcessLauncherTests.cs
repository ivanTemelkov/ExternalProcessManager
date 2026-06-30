using IvTem.ExternalProcessManager.Configuration;
using IvTem.ExternalProcessManager.Lifecycle;

namespace IvTem.ExternalProcessManager.Tests.Lifecycle;

public sealed class WindowsProcessLauncherTests
{
    [Fact]
    public async Task LaunchReturnsDisposableHandleAndObservesExit()
    {
        WindowsProcessLauncher launcher = new();

        using IProcessHandle handle = launcher.Launch(new EffectiveExternalProcessConfiguration
        {
            Path = "ExternalProcessManager:Processes:0",
            Alias = "worker",
            AliasKey = "worker",
            FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
            ArgumentMode = EffectiveProcessArgumentMode.ArgumentList,
            ArgumentList = ["/d", "/c", "exit", "7"],
            Restart = new EffectiveRestartConfiguration
            {
                Mode = ExternalProcessRestartMode.NonZeroExitCode,
                MinBackoff = TimeSpan.FromSeconds(2),
                MaxBackoff = TimeSpan.FromMinutes(1),
                StableRunDuration = TimeSpan.FromMinutes(5),
                GracefulStopTimeout = TimeSpan.FromSeconds(10),
            },
        });

        ProcessExitResult exit = await handle.Exited.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(handle.ProcessId > 0);
        Assert.True(handle.StartedAt <= DateTimeOffset.Now);
        Assert.Equal(7, exit.ExitCode);
        Assert.True(exit.ExitedAt >= handle.StartedAt);
    }
}
