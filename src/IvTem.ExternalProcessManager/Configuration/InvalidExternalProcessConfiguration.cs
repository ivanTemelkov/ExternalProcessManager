using System.Collections.Immutable;

namespace IvTem.ExternalProcessManager.Configuration;

internal sealed record InvalidExternalProcessConfiguration
{
    public string? Alias { get; init; }

    public required string Path { get; init; }

    public ImmutableArray<ExternalProcessValidationError> ValidationErrors { get; init; } = [];
}
