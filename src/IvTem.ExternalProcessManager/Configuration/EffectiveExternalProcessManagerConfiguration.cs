using System.Collections.Immutable;

namespace IvTem.ExternalProcessManager.Configuration;

internal sealed record EffectiveExternalProcessManagerConfiguration
{
    public ImmutableArray<EffectiveExternalProcessConfiguration> Processes { get; init; } = [];

    public ImmutableArray<InvalidExternalProcessConfiguration> InvalidProcesses { get; init; } = [];

    public ImmutableArray<ExternalProcessValidationError> ValidationErrors { get; init; } = [];
}
