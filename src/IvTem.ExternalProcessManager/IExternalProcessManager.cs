namespace IvTem.ExternalProcessManager;

/// <summary>
/// Controls the lifecycle of configured external processes and exposes diagnostics snapshots.
/// </summary>
public interface IExternalProcessManager : IAsyncDisposable
{
    /// <summary>
    /// Starts supervision for all valid configured processes.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel startup.</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops all managed processes and ends supervision.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel graceful shutdown.</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest immutable diagnostics snapshot.
    /// </summary>
    ExternalProcessManagerSnapshot GetSnapshot();
}
