using IvTem.ExternalProcessManager.Configuration;
using Microsoft.Extensions.Configuration;

namespace IvTem.ExternalProcessManager.Tests.Configuration;

public sealed class ExternalProcessConfigurationValidatorTests
{
    [Fact]
    public void ValidMinimalEntryBecomesEffectiveConfig()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
        };

        EffectiveExternalProcessManagerConfiguration configuration = Validate(values);

        EffectiveExternalProcessConfiguration process = Assert.Single(configuration.Processes);
        Assert.Empty(configuration.InvalidProcesses);
        Assert.Empty(configuration.ValidationErrors);
        Assert.Equal("worker-a", process.Alias);
        Assert.Equal("worker-a", process.AliasKey);
        Assert.Equal("worker-a.exe", process.FileName);
        Assert.Equal(EffectiveProcessArgumentMode.None, process.ArgumentMode);
        Assert.Equal(ExternalProcessRestartMode.NonZeroExitCode, process.Restart.Mode);
    }

    [Fact]
    public void DuplicateAliasesFailCaseInsensitively()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
            ["ExternalProcessManager:Processes:1:Alias"] = "WORKER-A",
            ["ExternalProcessManager:Processes:1:FileName"] = "worker-b.exe",
        };

        EffectiveExternalProcessManagerConfiguration configuration = Validate(values);

        Assert.Empty(configuration.Processes);
        Assert.Equal(2, configuration.InvalidProcesses.Length);
        Assert.All(configuration.ValidationErrors, error => Assert.Equal("Alias must be unique.", error.Message));
    }

    [Fact]
    public void MissingAliasFails()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
        };

        EffectiveExternalProcessManagerConfiguration configuration = Validate(values);

        InvalidExternalProcessConfiguration invalidProcess = Assert.Single(configuration.InvalidProcesses);
        ExternalProcessValidationError error = Assert.Single(invalidProcess.ValidationErrors);
        Assert.Null(error.Alias);
        Assert.Equal("ExternalProcessManager:Processes:0:Alias", error.Path);
        Assert.Equal("Alias is required.", error.Message);
    }

    [Fact]
    public void MissingFileNameFails()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
        };

        EffectiveExternalProcessManagerConfiguration configuration = Validate(values);

        InvalidExternalProcessConfiguration invalidProcess = Assert.Single(configuration.InvalidProcesses);
        ExternalProcessValidationError error = Assert.Single(invalidProcess.ValidationErrors);
        Assert.Equal("worker-a", error.Alias);
        Assert.Equal("ExternalProcessManager:Processes:0:FileName", error.Path);
        Assert.Equal("FileName is required.", error.Message);
    }

    [Fact]
    public void InvalidRestartModeFails()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
            ["ExternalProcessManager:Processes:0:Restart:Mode"] = "Sometimes",
        };

        EffectiveExternalProcessManagerConfiguration configuration = Validate(values);

        ExternalProcessValidationError error = Assert.Single(configuration.ValidationErrors);
        Assert.Equal("worker-a", error.Alias);
        Assert.Equal("ExternalProcessManager:Processes:0:Restart:Mode", error.Path);
        Assert.Equal("Restart mode must be NonZeroExitCode, Always, or Never.", error.Message);
    }

    [Fact]
    public void InvalidDurationFails()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
            ["ExternalProcessManager:Processes:0:Restart:MinBackoff"] = "not-a-duration",
        };

        EffectiveExternalProcessManagerConfiguration configuration = Validate(values);

        ExternalProcessValidationError error = Assert.Single(configuration.ValidationErrors);
        Assert.Equal("worker-a", error.Alias);
        Assert.Equal("ExternalProcessManager:Processes:0:Restart:MinBackoff", error.Path);
        Assert.Equal("MinBackoff must be a valid TimeSpan.", error.Message);
    }

    [Fact]
    public void ZeroOrNegativeBackoffFails()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
            ["ExternalProcessManager:Processes:0:Restart:MinBackoff"] = "00:00:00",
            ["ExternalProcessManager:Processes:0:Restart:MaxBackoff"] = "-00:00:01",
        };

        EffectiveExternalProcessManagerConfiguration configuration = Validate(values);

        Assert.Equal(2, configuration.ValidationErrors.Length);
        Assert.Contains(
            configuration.ValidationErrors,
            error => string.Equals(error.Message, "MinBackoff must be greater than zero.", StringComparison.Ordinal));
        Assert.Contains(
            configuration.ValidationErrors,
            error => string.Equals(error.Message, "MaxBackoff must be greater than zero.", StringComparison.Ordinal));
    }

    [Fact]
    public void MinBackoffGreaterThanMaxBackoffFails()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
            ["ExternalProcessManager:Processes:0:Restart:MinBackoff"] = "00:02:00",
            ["ExternalProcessManager:Processes:0:Restart:MaxBackoff"] = "00:01:00",
        };

        EffectiveExternalProcessManagerConfiguration configuration = Validate(values);

        ExternalProcessValidationError error = Assert.Single(configuration.ValidationErrors);
        Assert.Equal("worker-a", error.Alias);
        Assert.Equal("ExternalProcessManager:Processes:0:Restart:MinBackoff", error.Path);
        Assert.Equal("MinBackoff must be less than or equal to MaxBackoff.", error.Message);
    }

    [Fact]
    public void DefaultsAreApplied()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
        };

        EffectiveExternalProcessManagerConfiguration configuration = Validate(values);

        EffectiveRestartConfiguration restart = Assert.Single(configuration.Processes).Restart;
        Assert.Equal(ExternalProcessRestartMode.NonZeroExitCode, restart.Mode);
        Assert.Equal(TimeSpan.FromSeconds(2), restart.MinBackoff);
        Assert.Equal(TimeSpan.FromMinutes(1), restart.MaxBackoff);
        Assert.Equal(TimeSpan.FromMinutes(5), restart.StableRunDuration);
        Assert.Equal(TimeSpan.FromSeconds(10), restart.GracefulStopTimeout);
    }

    [Fact]
    public void ArgumentListIsPreferredOverArguments()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
            ["ExternalProcessManager:Processes:0:Arguments"] = "--port 5050",
            ["ExternalProcessManager:Processes:0:ArgumentList:0"] = "--port",
            ["ExternalProcessManager:Processes:0:ArgumentList:1"] = "5050",
        };

        EffectiveExternalProcessManagerConfiguration configuration = Validate(values);

        EffectiveExternalProcessConfiguration process = Assert.Single(configuration.Processes);
        Assert.Equal(EffectiveProcessArgumentMode.ArgumentList, process.ArgumentMode);
        Assert.Null(process.Arguments);
        Assert.Collection(
            process.ArgumentList,
            argument => Assert.Equal("--port", argument),
            argument => Assert.Equal("5050", argument));
    }

    [Fact]
    public void ValidAndInvalidEntriesAreReturnedTogether()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = "worker-a.exe",
            ["ExternalProcessManager:Processes:1:Alias"] = "worker-b",
        };

        EffectiveExternalProcessManagerConfiguration configuration = Validate(values);

        Assert.Single(configuration.Processes);
        Assert.Single(configuration.InvalidProcesses);
        Assert.Single(configuration.ValidationErrors);
        Assert.Equal("worker-a", configuration.Processes[0].Alias);
        Assert.Equal("worker-b", configuration.InvalidProcesses[0].Alias);
    }

    private static EffectiveExternalProcessManagerConfiguration Validate(Dictionary<string, string?> values)
    {
        IConfigurationSection section = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build()
            .GetSection("ExternalProcessManager");
        ExternalProcessConfigurationReader reader = new();
        ExternalProcessConfigurationValidator validator = new();

        return validator.Validate(reader.Read(section));
    }
}
