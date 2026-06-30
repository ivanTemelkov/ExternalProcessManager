using IvTem.ExternalProcessManager.Configuration;
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

        services.TryAddSingleton(new ExternalProcessManagerConfigurationSource(section));
        services.TryAddSingleton<ExternalProcessConfigurationReader>();
        services.TryAddSingleton<ExternalProcessConfigurationValidator>();
        services.TryAddSingleton<ILocalClock, SystemLocalClock>();
        services.TryAddSingleton<IProcessLauncher, WindowsProcessLauncher>();
        services.TryAddSingleton<IProcessCleanup, WindowsProcessCleanup>();
        services.TryAddSingleton<IRestartDelay, SystemRestartDelay>();
        services.TryAddSingleton<IScheduledRestartTimerFactory, SystemScheduledRestartTimerFactory>();
        services.TryAddSingleton<IExternalProcessSupervisorFactory, ExternalProcessSupervisorFactory>();
        services.TryAddSingleton<IExternalProcessManager, ExternalProcessManager>();

        return services;
    }
}
