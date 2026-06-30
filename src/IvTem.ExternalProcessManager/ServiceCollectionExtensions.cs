using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services.AddSingleton<IExternalProcessManager, ExternalProcessManager>();
    }
}
