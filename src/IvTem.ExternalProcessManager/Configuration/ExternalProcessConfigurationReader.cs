using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;

namespace IvTem.ExternalProcessManager.Configuration;

internal sealed class ExternalProcessConfigurationReader
{
    public RawExternalProcessManagerConfiguration Read(IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(section);

        IConfigurationSection processesSection = section.GetSection(
            nameof(RawExternalProcessManagerConfiguration.Processes));

        return new RawExternalProcessManagerConfiguration
        {
            Path = section is IConfigurationSection configurationSection ? configurationSection.Path : string.Empty,
            ProcessesPath = processesSection.Path,
            Processes = [.. processesSection.GetChildren().Select(ReadProcess)],
        };
    }

    private static RawExternalProcessConfiguration ReadProcess(IConfigurationSection section)
    {
        return new RawExternalProcessConfiguration
        {
            Path = section.Path,
            Alias = ReadValue(section, nameof(RawExternalProcessConfiguration.Alias)),
            FileName = ReadValue(section, nameof(RawExternalProcessConfiguration.FileName)),
            Arguments = ReadValue(section, nameof(RawExternalProcessConfiguration.Arguments)),
            ArgumentList = ReadValueArray(section.GetSection(nameof(RawExternalProcessConfiguration.ArgumentList))),
            WorkingDirectory = ReadValue(section, nameof(RawExternalProcessConfiguration.WorkingDirectory)),
            Environment = ReadEnvironment(section.GetSection(nameof(RawExternalProcessConfiguration.Environment))),
            Restart = ReadOptionalRestart(section.GetSection(nameof(RawExternalProcessConfiguration.Restart))),
            ScheduledRestarts =
            [
                .. section
                    .GetSection(nameof(RawExternalProcessConfiguration.ScheduledRestarts))
                    .GetChildren()
                    .Select(ReadScheduledRestart),
            ],
        };
    }

    private static RawRestartConfiguration? ReadOptionalRestart(IConfigurationSection section)
    {
        if (SectionExists(section) == false)
            return null;

        return new RawRestartConfiguration
        {
            Path = section.Path,
            Mode = ReadValue(section, nameof(RawRestartConfiguration.Mode)),
            MinBackoffSeconds = ReadValue(section, nameof(RawRestartConfiguration.MinBackoffSeconds)),
            MaxBackoffSeconds = ReadValue(section, nameof(RawRestartConfiguration.MaxBackoffSeconds)),
            StableRunDurationSeconds = ReadValue(section, nameof(RawRestartConfiguration.StableRunDurationSeconds)),
            GracefulStopTimeoutSeconds = ReadValue(section, nameof(RawRestartConfiguration.GracefulStopTimeoutSeconds)),
        };
    }

    private static RawScheduledRestartConfiguration ReadScheduledRestart(IConfigurationSection section)
    {
        IConfigurationSection dayOfWeekSection = section.GetSection(
            nameof(RawScheduledRestartConfiguration.DayOfWeek));

        return new RawScheduledRestartConfiguration
        {
            Path = section.Path,
            HourOfDay = ReadValue(section, nameof(RawScheduledRestartConfiguration.HourOfDay)),
            DayOfWeek = ToRawValue(dayOfWeekSection),
            DayOfWeekValues = ReadValueArray(dayOfWeekSection),
        };
    }

    private static ImmutableDictionary<string, RawConfigurationValue> ReadEnvironment(IConfigurationSection section)
        => section
            .GetChildren()
            .ToImmutableDictionary(
                child => child.Key,
                ToRawValue,
                StringComparer.OrdinalIgnoreCase);

    private static ImmutableArray<RawConfigurationValue> ReadValueArray(IConfigurationSection section)
        => [.. section.GetChildren().Select(ToRawValue)];

    private static RawConfigurationValue ReadValue(IConfigurationSection section, string key)
        => ToRawValue(section.GetSection(key));

    private static RawConfigurationValue ToRawValue(IConfigurationSection section)
        => new()
        {
            Path = section.Path,
            Value = section.Value,
        };

    private static bool SectionExists(IConfigurationSection section)
        => section.Value is not null || section.GetChildren().Any();
}
