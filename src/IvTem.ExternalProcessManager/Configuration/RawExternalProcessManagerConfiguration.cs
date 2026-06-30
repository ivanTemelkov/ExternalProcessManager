using System.Collections.Immutable;

namespace IvTem.ExternalProcessManager.Configuration;

internal sealed record RawExternalProcessManagerConfiguration
{
    public required string Path { get; init; }

    public required string ProcessesPath { get; init; }

    public ImmutableArray<RawExternalProcessConfiguration> Processes { get; init; } = [];
}
