namespace IvTem.ExternalProcessManager.Lifecycle;

internal sealed class SystemRestartDelay : IRestartDelay
{
    public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
        => Task.Delay(delay, cancellationToken);
}
