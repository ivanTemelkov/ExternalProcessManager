namespace IvTem.ExternalProcessManager.Configuration;

internal sealed record RawConfigurationValue
{
    public required string Path { get; init; }

    public string? Value { get; init; }
}
