using System.Diagnostics;
using System.Globalization;

if (args.Length == 0)
    return 1;

return await Run(args).ConfigureAwait(continueOnCapturedContext: false);

// Modes:
// - exit --exit-code <code>
// - delay-exit --delay-ms <milliseconds> --exit-code <code>
// - run-until-killed [--ready-file <path>]
// - spawn-child [--ready-file <path>] [--child-pid-file <path>]
// - handle-ctrl-break [--ready-file <path>] [--stopped-file <path>]
// - ignore-ctrl-break [--ready-file <path>]
// Compatibility mode:
// - spawn-child-ignore-ctrl-break [--ready-file <path>] [--child-pid-file <path>]
static async Task<int> Run(string[] args)
{
    string mode = args[0];
    Dictionary<string, string> options = ParseOptions(args);

    return mode switch
    {
        "exit" => GetExitCode(options),
        "delay-exit" => await DelayExit(options).ConfigureAwait(continueOnCapturedContext: false),
        "run-until-killed" => await RunUntilKilled(options).ConfigureAwait(continueOnCapturedContext: false),
        "spawn-child" => await SpawnChild(options, "run-until-killed", ignoreCtrlBreak: false).ConfigureAwait(continueOnCapturedContext: false),
        "handle-ctrl-break" => await HandleCtrlBreak(options).ConfigureAwait(continueOnCapturedContext: false),
        "ignore-ctrl-break" => await IgnoreCtrlBreak(options).ConfigureAwait(continueOnCapturedContext: false),
        "spawn-child-ignore-ctrl-break" => await SpawnChild(options, "ignore-ctrl-break", ignoreCtrlBreak: true).ConfigureAwait(continueOnCapturedContext: false),
        _ => 2,
    };
}

static async Task<int> DelayExit(IReadOnlyDictionary<string, string> options)
{
    if (TryGetInt32(options, "delay-ms", 100, out int delayMilliseconds) == false
        || delayMilliseconds < 0)
    {
        return 3;
    }

    await Task.Delay(TimeSpan.FromMilliseconds(delayMilliseconds))
        .ConfigureAwait(continueOnCapturedContext: false);

    return GetExitCode(options);
}

static async Task<int> RunUntilKilled(IReadOnlyDictionary<string, string> options)
{
    WriteReadyFile(options);

    await Task.Delay(Timeout.InfiniteTimeSpan).ConfigureAwait(continueOnCapturedContext: false);
    return 0;
}

static async Task<int> HandleCtrlBreak(IReadOnlyDictionary<string, string> options)
{
    TaskCompletionSource<int> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    Console.CancelKeyPress += (_, eventArgs) =>
    {
        if (eventArgs.SpecialKey == ConsoleSpecialKey.ControlBreak)
        {
            eventArgs.Cancel = true;

            if (options.TryGetValue("stopped-file", out string? stoppedFile))
                File.WriteAllText(stoppedFile, "ctrl-break");

            completion.TrySetResult(0);
        }
    };

    WriteReadyFile(options);

    return await completion.Task.ConfigureAwait(continueOnCapturedContext: false);
}

static async Task<int> IgnoreCtrlBreak(IReadOnlyDictionary<string, string> options)
{
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        if (eventArgs.SpecialKey == ConsoleSpecialKey.ControlBreak)
            eventArgs.Cancel = true;
    };

    WriteReadyFile(options);

    await Task.Delay(Timeout.InfiniteTimeSpan).ConfigureAwait(continueOnCapturedContext: false);
    return 0;
}

static async Task<int> SpawnChild(
    IReadOnlyDictionary<string, string> options,
    string childMode,
    bool ignoreCtrlBreak)
{
    string? childPidFile = options.TryGetValue("child-pid-file", out string? configuredChildPidFile)
        ? configuredChildPidFile
        : null;

    using Process child = Process.Start(new ProcessStartInfo
    {
        FileName = Environment.ProcessPath ?? throw new InvalidOperationException("Process path is unavailable."),
        UseShellExecute = false,
        ArgumentList =
        {
            childMode,
        },
    }) ?? throw new InvalidOperationException("Child helper process did not start.");

    if (childPidFile is not null)
    {
        await File.WriteAllTextAsync(childPidFile, child.Id.ToString(CultureInfo.InvariantCulture))
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    if (ignoreCtrlBreak)
        return await IgnoreCtrlBreak(options).ConfigureAwait(continueOnCapturedContext: false);

    await RunUntilKilled(options).ConfigureAwait(continueOnCapturedContext: false);
    return 0;
}

static Dictionary<string, string> ParseOptions(string[] args)
{
    Dictionary<string, string> options = new(StringComparer.OrdinalIgnoreCase);

    for (int index = 1; index < args.Length - 1; index += 2)
    {
        string key = args[index];

        if (key.StartsWith("--", StringComparison.Ordinal) == false)
            continue;

        options[key[2..]] = args[index + 1];
    }

    return options;
}

static void WriteReadyFile(IReadOnlyDictionary<string, string> options)
{
    if (options.TryGetValue("ready-file", out string? readyFile))
        File.WriteAllText(readyFile, "ready");
}

static int GetExitCode(IReadOnlyDictionary<string, string> options)
{
    return TryGetInt32(options, "exit-code", 0, out int exitCode)
        ? exitCode
        : 3;
}

static bool TryGetInt32(
    IReadOnlyDictionary<string, string> options,
    string key,
    int defaultValue,
    out int value)
{
    if (options.TryGetValue(key, out string? configuredValue) == false)
    {
        value = defaultValue;
        return true;
    }

    return int.TryParse(configuredValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}
