namespace IvTem.ExternalProcessManager.Configuration;

internal sealed record RawRestartConfiguration
{
    public required string Path { get; init; }

    public required RawConfigurationValue Mode { get; init; }

    public required RawConfigurationValue MinBackoff { get; init; }

    public required RawConfigurationValue MaxBackoff { get; init; }

    public required RawConfigurationValue StableRunDuration { get; init; }

    public required RawConfigurationValue GracefulStopTimeout { get; init; }
}
