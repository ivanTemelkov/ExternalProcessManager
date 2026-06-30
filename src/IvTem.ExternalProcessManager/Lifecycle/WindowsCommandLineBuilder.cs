using System.Collections.ObjectModel;
using System.Text;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal static class WindowsCommandLineBuilder
{
    public static string Build(string fileName, string arguments, Collection<string> argumentList)
    {
        StringBuilder commandLine = new();
        AppendArgument(commandLine, fileName);

        if (argumentList.Count > 0)
        {
            foreach (string argument in argumentList)
            {
                commandLine.Append(' ');
                AppendArgument(commandLine, argument);
            }

            return commandLine.ToString();
        }

        if (string.IsNullOrWhiteSpace(arguments) == false)
        {
            commandLine.Append(' ');
            commandLine.Append(arguments);
        }

        return commandLine.ToString();
    }

    private static void AppendArgument(StringBuilder commandLine, string argument)
    {
        if (argument.Length == 0)
        {
            commandLine.Append("\"\"");
            return;
        }

        bool requiresQuotes = RequiresQuotes(argument);

        if (requiresQuotes == false)
        {
            commandLine.Append(argument);
            return;
        }

        commandLine.Append('"');

        int backslashCount = 0;
        foreach (char character in argument)
        {
            if (character == '\\')
            {
                backslashCount++;
                continue;
            }

            if (character == '"')
            {
                commandLine.Append('\\', backslashCount * 2 + 1);
                commandLine.Append('"');
                backslashCount = 0;
                continue;
            }

            commandLine.Append('\\', backslashCount);
            commandLine.Append(character);
            backslashCount = 0;
        }

        commandLine.Append('\\', backslashCount * 2);
        commandLine.Append('"');
    }

    private static bool RequiresQuotes(string argument)
    {
        foreach (char character in argument)
        {
            if (char.IsWhiteSpace(character) || character == '"')
                return true;
        }

        return false;
    }
}
