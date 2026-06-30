using System.Collections.Immutable;

namespace IvTem.ExternalProcessManager;

/// <summary>
/// Immutable diagnostics for one configured external process alias.
/// </summary>
public sealed record ExternalProcessSnapshot
{
    public required string Alias { get; init; }

    public required ExternalProcessStatus Status { get; init; }

    public string? FileName { get; init; }

    public ImmutableArray<string> Arguments { get; init; } = [];

    public string? WorkingDirectory { get; init; }

    public int? ProcessId { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? ExitedAt { get; init; }

    public int? LastExitCode { get; init; }

    public int RestartCount { get; init; }

    public DateTimeOffset? NextScheduledRestart { get; init; }

    public string? LastError { get; init; }

    public ImmutableArray<ExternalProcessValidationError> ValidationErrors { get; init; } = [];
}
