namespace IvTem.ExternalProcessManager.Lifecycle;

internal sealed record ProcessCleanupResult
{
    public required int ProcessId { get; init; }

    public required ProcessCleanupOutcome Outcome { get; init; }

    public int? ExitCode { get; init; }

    public required DateTimeOffset CompletedAt { get; init; }
}
