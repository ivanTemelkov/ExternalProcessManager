using IvTem.ExternalProcessManager.Configuration;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal interface IExternalProcessSupervisorFactory
{
    IExternalProcessSupervisor Create(EffectiveExternalProcessConfiguration configuration);
}
