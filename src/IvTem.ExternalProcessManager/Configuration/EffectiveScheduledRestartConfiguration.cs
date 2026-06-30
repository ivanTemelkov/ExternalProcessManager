using System.Collections.Immutable;

namespace IvTem.ExternalProcessManager.Configuration;

internal sealed record EffectiveScheduledRestartConfiguration
{
    public required string Path { get; init; }

    public string? HourOfDay { get; init; }

    public string? DayOfWeek { get; init; }

    public ImmutableArray<string> DayOfWeekValues { get; init; } = [];
}
