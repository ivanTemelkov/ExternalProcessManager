using IvTem.ExternalProcessManager.Configuration;
using Microsoft.Extensions.Configuration;

namespace IvTem.ExternalProcessManager.Tests.Configuration;

public sealed class ExternalProcessConfigurationReaderTests
{
    [Fact]
    public void MissingRootSectionReturnsNoProcesses()
    {
        IConfigurationSection section = BuildConfiguration([]).GetSection("ExternalProcessManager");
        ExternalProcessConfigurationReader reader = new();

        RawExternalProcessManagerConfiguration configuration = reader.Read(section);

        Assert.Empty(configuration.Processes);
        Assert.Equal("ExternalProcessManager:Processes", configuration.ProcessesPath);
    }

    [Fact]
    public void EmptyProcessesSectionReturnsNoProcesses()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes"] = string.Empty,
        };
        IConfigurationSection section = BuildConfiguration(values).GetSection("ExternalProcessManager");
        ExternalProcessConfigurationReader reader = new();

        RawExternalProcessManagerConfiguration configuration = reader.Read(section);

        Assert.Empty(configuration.Processes);
    }

    [Fact]
    public void MinimalProcessReadsAliasAndFileName()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
        };
        IConfigurationSection section = BuildConfiguration(values).GetSection("ExternalProcessManager");
        ExternalProcessConfigurationReader reader = new();

        RawExternalProcessConfiguration process = reader.Read(section).Processes[0];

        Assert.Equal("ExternalProcessManager:Processes:0", process.Path);
        Assert.Equal("worker-a", process.Alias.Value);
        Assert.Equal("ExternalProcessManager:Processes:0:Alias", process.Alias.Path);
        Assert.Equal("worker-a.exe", process.FileName.Value);
        Assert.Equal("ExternalProcessManager:Processes:0:FileName", process.FileName.Path);
    }

    [Fact]
    public void ArgumentsOnlyIsRead()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
            ["ExternalProcessManager:Processes:0:Arguments"] = "--port 5050",
        };
        IConfigurationSection section = BuildConfiguration(values).GetSection("ExternalProcessManager");
        ExternalProcessConfigurationReader reader = new();

        RawExternalProcessConfiguration process = reader.Read(section).Processes[0];

        Assert.Equal("--port 5050", process.Arguments.Value);
        Assert.Empty(process.ArgumentList);
    }

    [Fact]
    public void ArgumentListOnlyIsRead()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
            ["ExternalProcessManager:Processes:0:ArgumentList:0"] = "--port",
            ["ExternalProcessManager:Processes:0:ArgumentList:1"] = "5050",
        };
        IConfigurationSection section = BuildConfiguration(values).GetSection("ExternalProcessManager");
        ExternalProcessConfigurationReader reader = new();

        RawExternalProcessConfiguration process = reader.Read(section).Processes[0];

        Assert.Null(process.Arguments.Value);
        Assert.Collection(
            process.ArgumentList,
            argument => Assert.Equal("--port", argument.Value),
            argument => Assert.Equal("5050", argument.Value));
    }

    [Fact]
    public void ArgumentsAndArgumentListAreBothRead()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
            ["ExternalProcessManager:Processes:0:Arguments"] = "--port 5050",
            ["ExternalProcessManager:Processes:0:ArgumentList:0"] = "--port",
            ["ExternalProcessManager:Processes:0:ArgumentList:1"] = "5050",
        };
        IConfigurationSection section = BuildConfiguration(values).GetSection("ExternalProcessManager");
        ExternalProcessConfigurationReader reader = new();

        RawExternalProcessConfiguration process = reader.Read(section).Processes[0];

        Assert.Equal("--port 5050", process.Arguments.Value);
        Assert.Equal("--port", process.ArgumentList[0].Value);
        Assert.Equal("5050", process.ArgumentList[1].Value);
    }

    [Fact]
    public void EnvironmentDictionaryIsRead()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
            ["ExternalProcessManager:Processes:0:Environment:DOTNET_ENVIRONMENT"] = "Production",
            ["ExternalProcessManager:Processes:0:Environment:FeatureFlag"] = "Enabled",
        };
        IConfigurationSection section = BuildConfiguration(values).GetSection("ExternalProcessManager");
        ExternalProcessConfigurationReader reader = new();

        RawExternalProcessConfiguration process = reader.Read(section).Processes[0];

        Assert.Equal("Production", process.Environment["DOTNET_ENVIRONMENT"].Value);
        Assert.Equal("Enabled", process.Environment["featureflag"].Value);
        Assert.Equal(
            "ExternalProcessManager:Processes:0:Environment:DOTNET_ENVIRONMENT",
            process.Environment["DOTNET_ENVIRONMENT"].Path);
    }

    [Fact]
    public void WorkingDirectoryIsRead()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
            ["ExternalProcessManager:Processes:0:WorkingDirectory"] = "C:\\apps",
        };
        IConfigurationSection section = BuildConfiguration(values).GetSection("ExternalProcessManager");
        ExternalProcessConfigurationReader reader = new();

        RawExternalProcessConfiguration process = reader.Read(section).Processes[0];

        Assert.Equal("C:\\apps", process.WorkingDirectory.Value);
        Assert.Equal("ExternalProcessManager:Processes:0:WorkingDirectory", process.WorkingDirectory.Path);
    }

    [Fact]
    public void RestartAndScheduledRestartChildSectionsAreRead()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
            ["ExternalProcessManager:Processes:0:Restart:Mode"] = "Always",
            ["ExternalProcessManager:Processes:0:Restart:MinBackoff"] = "00:00:02",
            ["ExternalProcessManager:Processes:0:Restart:MaxBackoff"] = "00:01:00",
            ["ExternalProcessManager:Processes:0:Restart:StableRunDuration"] = "00:05:00",
            ["ExternalProcessManager:Processes:0:Restart:GracefulStopTimeout"] = "00:00:10",
            ["ExternalProcessManager:Processes:0:ScheduledRestarts:0:HourOfDay"] = "23:45",
            ["ExternalProcessManager:Processes:0:ScheduledRestarts:0:DayOfWeek"] = "Monday|Friday",
            ["ExternalProcessManager:Processes:0:ScheduledRestarts:1:HourOfDay"] = "04:00",
            ["ExternalProcessManager:Processes:0:ScheduledRestarts:1:DayOfWeek:0"] = "Tuesday",
            ["ExternalProcessManager:Processes:0:ScheduledRestarts:1:DayOfWeek:1"] = "Thursday",
        };
        IConfigurationSection section = BuildConfiguration(values).GetSection("ExternalProcessManager");
        ExternalProcessConfigurationReader reader = new();

        RawExternalProcessConfiguration process = reader.Read(section).Processes[0];

        Assert.NotNull(process.Restart);
        Assert.Equal("Always", process.Restart.Mode.Value);
        Assert.Equal("00:00:02", process.Restart.MinBackoff.Value);
        Assert.Equal("00:01:00", process.Restart.MaxBackoff.Value);
        Assert.Equal("00:05:00", process.Restart.StableRunDuration.Value);
        Assert.Equal("00:00:10", process.Restart.GracefulStopTimeout.Value);
        Assert.Collection(
            process.ScheduledRestarts,
            schedule =>
            {
                Assert.Equal("23:45", schedule.HourOfDay.Value);
                Assert.Equal("Monday|Friday", schedule.DayOfWeek.Value);
                Assert.Empty(schedule.DayOfWeekValues);
            },
            schedule =>
            {
                Assert.Equal("04:00", schedule.HourOfDay.Value);
                Assert.Null(schedule.DayOfWeek.Value);
                Assert.Equal("Tuesday", schedule.DayOfWeekValues[0].Value);
                Assert.Equal("Thursday", schedule.DayOfWeekValues[1].Value);
            });
    }

    private static IConfigurationRoot BuildConfiguration(Dictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
