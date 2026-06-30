using IvTem.ExternalProcessManager.Lifecycle;
using IvTem.ExternalProcessManager.Scheduling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IvTem.ExternalProcessManager;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers external process manager services for the supplied configuration section.
    /// </summary>
    public static IServiceCollection AddExternalProcessManager(
        this IServiceCollection services,
        IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(section);

        services.TryAddSingleton<ILocalClock, SystemLocalClock>();
        services.TryAddSingleton<IProcessLauncher, WindowsProcessLauncher>();
        services.TryAddSingleton<IExternalProcessManager, ExternalProcessManager>();

        return services;
    }
}
