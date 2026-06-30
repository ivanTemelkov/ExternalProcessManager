namespace IvTem.ExternalProcessManager.Configuration;

internal sealed record EffectiveRestartConfiguration
{
    public required ExternalProcessRestartMode Mode { get; init; }

    public required TimeSpan MinBackoff { get; init; }

    public required TimeSpan MaxBackoff { get; init; }

    public required TimeSpan StableRunDuration { get; init; }

    public required TimeSpan GracefulStopTimeout { get; init; }
}
