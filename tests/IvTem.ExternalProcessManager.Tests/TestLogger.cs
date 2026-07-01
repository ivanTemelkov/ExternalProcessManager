using Microsoft.Extensions.Logging;

namespace IvTem.ExternalProcessManager.Tests;

internal sealed class TestLogger<T> : ILogger<T>
{
    public List<TestLogEntry> Entries { get; } = [];

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
        => TestLogScope.Instance;

    public bool IsEnabled(LogLevel logLevel)
        => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        Entries.Add(new TestLogEntry
        {
            Level = logLevel,
            EventId = eventId.Id,
            Message = formatter(state, exception),
            Exception = exception,
        });
    }

}

internal sealed record TestLogEntry
{
    public required LogLevel Level { get; init; }

    public required int EventId { get; init; }

    public required string Message { get; init; }

    public Exception? Exception { get; init; }
}

internal sealed class TestLogScope : IDisposable
{
    public static TestLogScope Instance { get; } = new();

    public void Dispose()
    {
    }
}
