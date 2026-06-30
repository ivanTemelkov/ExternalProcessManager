using System.Collections.Immutable;

namespace IvTem.ExternalProcessManager.Configuration;

internal sealed record RawScheduledRestartConfiguration
{
    public required string Path { get; init; }

    public required RawConfigurationValue HourOfDay { get; init; }

    public required RawConfigurationValue DayOfWeek { get; init; }

    public ImmutableArray<RawConfigurationValue> DayOfWeekValues { get; init; } = [];
}
