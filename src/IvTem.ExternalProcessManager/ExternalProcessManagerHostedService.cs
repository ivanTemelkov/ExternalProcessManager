using Microsoft.Extensions.Hosting;

namespace IvTem.ExternalProcessManager;

internal sealed class ExternalProcessManagerHostedService : IHostedService
{
    public ExternalProcessManagerHostedService(IExternalProcessManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        Manager = manager;
    }

    private IExternalProcessManager Manager { get; }

    public async Task StartAsync(CancellationToken cancellationToken)
        => await Manager.StartAsync(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

    public async Task StopAsync(CancellationToken cancellationToken)
        => await Manager.StopAsync(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
}
