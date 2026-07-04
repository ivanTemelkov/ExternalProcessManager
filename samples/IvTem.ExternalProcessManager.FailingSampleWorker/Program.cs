using System.Globalization;

const int ExitCode = 42;

Console.Error.WriteLine(
    "Failing sample worker exiting immediately with status {0}. Process ID: {1}.",
    ExitCode.ToString(CultureInfo.InvariantCulture),
    Environment.ProcessId.ToString(CultureInfo.InvariantCulture));

return ExitCode;
