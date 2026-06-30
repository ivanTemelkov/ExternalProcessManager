using IvTem.ExternalProcessManager.Configuration;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal interface IProcessLauncher
{
    IProcessHandle Launch(EffectiveExternalProcessConfiguration configuration);
}
