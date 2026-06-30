using System.Collections.Immutable;

namespace IvTem.ExternalProcessManager.Configuration;

internal sealed record RawExternalProcessConfiguration
{
    public required string Path { get; init; }

    public required RawConfigurationValue Alias { get; init; }

    public required RawConfigurationValue FileName { get; init; }

    public required RawConfigurationValue Arguments { get; init; }

    public ImmutableArray<RawConfigurationValue> ArgumentList { get; init; } = [];

    public required RawConfigurationValue WorkingDirectory { get; init; }

    public ImmutableDictionary<string, RawConfigurationValue> Environment { get; init; } =
        ImmutableDictionary<string, RawConfigurationValue>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

    public RawRestartConfiguration? Restart { get; init; }

    public ImmutableArray<RawScheduledRestartConfiguration> ScheduledRestarts { get; init; } = [];
}
