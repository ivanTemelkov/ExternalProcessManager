namespace IvTem.ExternalProcessManager.Configuration;

internal sealed record RawRestartConfiguration
{
    public required string Path { get; init; }

    public required RawConfigurationValue Mode { get; init; }

    public required RawConfigurationValue MinBackoffSeconds { get; init; }

    public required RawConfigurationValue MaxBackoffSeconds { get; init; }

    public required RawConfigurationValue StableRunDurationSeconds { get; init; }

    public required RawConfigurationValue GracefulStopTimeoutSeconds { get; init; }
}
