namespace IvTem.ExternalProcessManager.Lifecycle;

internal interface IRestartDelay
{
    Task Delay(TimeSpan delay, CancellationToken cancellationToken);
}
