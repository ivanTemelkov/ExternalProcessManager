namespace IvTem.ExternalProcessManager.Lifecycle;

internal interface IExternalProcessSupervisor : IDisposable
{
    ExternalProcessSnapshot GetSnapshot();

    Task Start(CancellationToken cancellationToken);

    Task Stop(CancellationToken cancellationToken);
}
