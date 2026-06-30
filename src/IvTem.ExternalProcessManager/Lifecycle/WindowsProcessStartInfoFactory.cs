using System.Diagnostics;
using IvTem.ExternalProcessManager.Configuration;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal static class WindowsProcessStartInfoFactory
{
    public static ProcessStartInfo Create(EffectiveExternalProcessConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ProcessStartInfo startInfo = new()
        {
            FileName = configuration.FileName,
            UseShellExecute = false,
        };

        ApplyArguments(configuration, startInfo);
        ApplyWorkingDirectory(configuration, startInfo);
        ApplyEnvironment(configuration, startInfo);

        return startInfo;
    }

    private static void ApplyArguments(
        EffectiveExternalProcessConfiguration configuration,
        ProcessStartInfo startInfo)
    {
        if (configuration.ArgumentMode == EffectiveProcessArgumentMode.RawString)
        {
            startInfo.Arguments = configuration.Arguments ?? string.Empty;
            return;
        }

        if (configuration.ArgumentMode == EffectiveProcessArgumentMode.ArgumentList)
        {
            foreach (string argument in configuration.ArgumentList)
            {
                startInfo.ArgumentList.Add(argument);
            }
        }
    }

    private static void ApplyWorkingDirectory(
        EffectiveExternalProcessConfiguration configuration,
        ProcessStartInfo startInfo)
    {
        if (configuration.WorkingDirectory is null)
            return;

        startInfo.WorkingDirectory = configuration.WorkingDirectory;
    }

    private static void ApplyEnvironment(
        EffectiveExternalProcessConfiguration configuration,
        ProcessStartInfo startInfo)
    {
        foreach (KeyValuePair<string, string> environmentVariable in configuration.Environment)
        {
            startInfo.Environment[environmentVariable.Key] = environmentVariable.Value;
        }
    }
}
