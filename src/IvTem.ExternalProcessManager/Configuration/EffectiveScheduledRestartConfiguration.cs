using System.Collections.Immutable;

namespace IvTem.ExternalProcessManager.Configuration;

internal sealed record EffectiveScheduledRestartConfiguration
{
    public required string Path { get; init; }

    public required TimeOnly HourOfDay { get; init; }

    public ImmutableArray<DayOfWeek> Days { get; init; } = [];
}
