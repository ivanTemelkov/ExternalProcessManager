using System.Collections.Immutable;

namespace IvTem.ExternalProcessManager.Configuration;

internal sealed record EffectiveExternalProcessConfiguration
{
    public required string Path { get; init; }

    public required string Alias { get; init; }

    public required string AliasKey { get; init; }

    public required string FileName { get; init; }

    public required EffectiveProcessArgumentMode ArgumentMode { get; init; }

    public string? Arguments { get; init; }

    public ImmutableArray<string> ArgumentList { get; init; } = [];

    public string? WorkingDirectory { get; init; }

    public ImmutableDictionary<string, string> Environment { get; init; } =
        ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

    public required EffectiveRestartConfiguration Restart { get; init; }

    public ImmutableArray<EffectiveScheduledRestartConfiguration> ScheduledRestarts { get; init; } = [];
}
