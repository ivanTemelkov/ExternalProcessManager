namespace IvTem.ExternalProcessManager;

internal sealed class ExternalProcessManager : IExternalProcessManager
{
    private Lock StateLock { get; } = new();

    private bool IsRunning { get; set; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (StateLock)
        {
            IsRunning = true;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (StateLock)
        {
            IsRunning = false;
        }

        return Task.CompletedTask;
    }

    public ExternalProcessManagerSnapshot GetSnapshot()
    {
        bool isRunning;

        lock (StateLock)
        {
            isRunning = IsRunning;
        }

        return new ExternalProcessManagerSnapshot
        {
            IsRunning = isRunning,
            GeneratedAt = DateTimeOffset.UtcNow,
        };
    }
}
