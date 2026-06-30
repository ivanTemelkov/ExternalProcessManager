namespace IvTem.ExternalProcessManager.Lifecycle;

internal sealed record ProcessExitResult
{
    public required int? ExitCode { get; init; }

    public required DateTimeOffset ExitedAt { get; init; }
}
