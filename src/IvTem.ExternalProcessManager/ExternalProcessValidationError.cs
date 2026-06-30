namespace IvTem.ExternalProcessManager;

/// <summary>
/// Describes a validation problem in the external process configuration.
/// </summary>
public sealed record ExternalProcessValidationError
{
    public string? Alias { get; init; }

    public required string Path { get; init; }

    public required string Message { get; init; }
}
