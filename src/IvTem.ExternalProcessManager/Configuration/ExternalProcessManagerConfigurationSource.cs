using Microsoft.Extensions.Configuration;

namespace IvTem.ExternalProcessManager.Configuration;

internal sealed class ExternalProcessManagerConfigurationSource
{
    public ExternalProcessManagerConfigurationSource(IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(section);

        Section = section;
    }

    public IConfiguration Section { get; }
}
