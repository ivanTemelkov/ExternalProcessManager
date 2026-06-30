using System.Collections.Immutable;
using System.Diagnostics;
using IvTem.ExternalProcessManager.Configuration;
using IvTem.ExternalProcessManager.Lifecycle;

namespace IvTem.ExternalProcessManager.Tests.Lifecycle;

public sealed class WindowsProcessStartInfoFactoryTests
{
    [Fact]
    public void FileNameMapsCorrectly()
    {
        ProcessStartInfo startInfo = WindowsProcessStartInfoFactory.Create(CreateConfiguration());

        Assert.Equal("worker.exe", startInfo.FileName);
        Assert.False(startInfo.UseShellExecute);
    }

    [Fact]
    public void RawArgumentsMapCorrectly()
    {
        EffectiveExternalProcessConfiguration configuration = CreateConfiguration(
            argumentMode: EffectiveProcessArgumentMode.RawString,
            arguments: "--port 5050");

        ProcessStartInfo startInfo = WindowsProcessStartInfoFactory.Create(configuration);

        Assert.Equal("--port 5050", startInfo.Arguments);
        Assert.Empty(startInfo.ArgumentList);
    }

    [Fact]
    public void StructuredArgumentsMapCorrectly()
    {
        EffectiveExternalProcessConfiguration configuration = CreateConfiguration(
            argumentMode: EffectiveProcessArgumentMode.ArgumentList,
            argumentList: ["--port", "5050"]);

        ProcessStartInfo startInfo = WindowsProcessStartInfoFactory.Create(configuration);

        Assert.Equal(string.Empty, startInfo.Arguments);
        Assert.Collection(
            startInfo.ArgumentList,
            argument => Assert.Equal("--port", argument),
            argument => Assert.Equal("5050", argument));
    }

    [Fact]
    public void StructuredArgumentsWinOverRawArguments()
    {
        EffectiveExternalProcessConfiguration configuration = CreateConfiguration(
            argumentMode: EffectiveProcessArgumentMode.ArgumentList,
            arguments: "--ignored",
            argumentList: ["--port", "5050"]);

        ProcessStartInfo startInfo = WindowsProcessStartInfoFactory.Create(configuration);

        Assert.Equal(string.Empty, startInfo.Arguments);
        Assert.Collection(
            startInfo.ArgumentList,
            argument => Assert.Equal("--port", argument),
            argument => Assert.Equal("5050", argument));
    }

    [Fact]
    public void WorkingDirectoryMapsCorrectly()
    {
        EffectiveExternalProcessConfiguration configuration = CreateConfiguration(workingDirectory: @"C:\worker");

        ProcessStartInfo startInfo = WindowsProcessStartInfoFactory.Create(configuration);

        Assert.Equal(@"C:\worker", startInfo.WorkingDirectory);
    }

    [Fact]
    public void EnvironmentOverridesMapCorrectly()
    {
        EffectiveExternalProcessConfiguration configuration = CreateConfiguration(
            environment: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["WORKER_MODE"] = "test",
                ["WORKER_EMPTY"] = string.Empty,
            });

        ProcessStartInfo startInfo = WindowsProcessStartInfoFactory.Create(configuration);

        Assert.Equal("test", startInfo.Environment["WORKER_MODE"]);
        Assert.Equal(string.Empty, startInfo.Environment["WORKER_EMPTY"]);
    }

    private static EffectiveExternalProcessConfiguration CreateConfiguration(
        EffectiveProcessArgumentMode argumentMode = EffectiveProcessArgumentMode.None,
        string? arguments = null,
        ImmutableArray<string> argumentList = default,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environment = null)
        => new()
        {
            Path = "ExternalProcessManager:Processes:0",
            Alias = "worker",
            AliasKey = "worker",
            FileName = "worker.exe",
            ArgumentMode = argumentMode,
            Arguments = arguments,
            ArgumentList = argumentList.IsDefault ? [] : argumentList,
            WorkingDirectory = workingDirectory,
            Environment = environment is null
                ? ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase)
                : environment.ToImmutableDictionary(
                    item => item.Key,
                    item => item.Value,
                    StringComparer.OrdinalIgnoreCase),
            Restart = new EffectiveRestartConfiguration
            {
                Mode = ExternalProcessRestartMode.NonZeroExitCode,
                MinBackoff = TimeSpan.FromSeconds(2),
                MaxBackoff = TimeSpan.FromMinutes(1),
                StableRunDuration = TimeSpan.FromMinutes(5),
                GracefulStopTimeout = TimeSpan.FromSeconds(10),
            },
        };
}
