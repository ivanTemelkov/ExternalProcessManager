using System.Diagnostics;

if (args.Length == 0)
    return 1;

return await Run(args).ConfigureAwait(continueOnCapturedContext: false);

static async Task<int> Run(string[] args)
{
    string mode = args[0];
    Dictionary<string, string> options = ParseOptions(args);

    return mode switch
    {
        "handle-ctrl-break" => await HandleCtrlBreak(options).ConfigureAwait(continueOnCapturedContext: false),
        "ignore-ctrl-break" => await IgnoreCtrlBreak(options).ConfigureAwait(continueOnCapturedContext: false),
        "spawn-child-ignore-ctrl-break" => await SpawnChildIgnoreCtrlBreak(options).ConfigureAwait(continueOnCapturedContext: false),
        _ => 2,
    };
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

static async Task<int> SpawnChildIgnoreCtrlBreak(IReadOnlyDictionary<string, string> options)
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
            "ignore-ctrl-break",
        },
    }) ?? throw new InvalidOperationException("Child helper process did not start.");

    if (childPidFile is not null)
        File.WriteAllText(childPidFile, child.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));

    await IgnoreCtrlBreak(options).ConfigureAwait(continueOnCapturedContext: false);
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
