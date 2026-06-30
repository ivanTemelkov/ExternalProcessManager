using System.Collections.Immutable;

namespace IvTem.ExternalProcessManager;

/// <summary>
/// Immutable diagnostics for the current external process manager state.
/// </summary>
public sealed record ExternalProcessManagerSnapshot
{
    public required bool IsRunning { get; init; }

    public required DateTimeOffset GeneratedAt { get; init; }

    public ImmutableArray<ExternalProcessSnapshot> Processes { get; init; } = [];

    public ImmutableArray<ExternalProcessValidationError> ValidationErrors { get; init; } = [];
}
