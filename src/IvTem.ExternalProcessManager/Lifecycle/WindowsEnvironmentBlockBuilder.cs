using System.Collections.Specialized;
using System.Text;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal static class WindowsEnvironmentBlockBuilder
{
    public static string Build(StringDictionary environment)
    {
        SortedDictionary<string, string> variables = new(StringComparer.OrdinalIgnoreCase);

        foreach (string? key in environment.Keys)
        {
            if (string.IsNullOrEmpty(key))
                continue;

            variables[key] = environment[key] ?? string.Empty;
        }

        StringBuilder block = new();
        foreach (KeyValuePair<string, string> variable in variables)
        {
            block.Append(variable.Key);
            block.Append('=');
            block.Append(variable.Value);
            block.Append('\0');
        }

        block.Append('\0');
        return block.ToString();
    }
}
