using System.Globalization;

Dictionary<string, string> options = ParseOptions(args);
string workerName = GetOption(options, "name", "sample-worker");
int heartbeatSeconds = GetPositiveInt32(options, "heartbeat-seconds", 2);

return await Run(workerName, heartbeatSeconds).ConfigureAwait(continueOnCapturedContext: false);

static async Task<int> Run(string workerName, int heartbeatSeconds)
{
    TaskCompletionSource<int> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    Console.CancelKeyPress += (_, eventArgs) =>
    {
        if (eventArgs.SpecialKey is ConsoleSpecialKey.ControlBreak or ConsoleSpecialKey.ControlC)
        {
            eventArgs.Cancel = true;
            Console.WriteLine("Sample worker received shutdown signal.");
            completion.TrySetResult(0);
        }
    };

    Console.WriteLine($"Sample worker {workerName} started with process ID {Environment.ProcessId.ToString(CultureInfo.InvariantCulture)}.");

    TimeSpan heartbeatInterval = TimeSpan.FromSeconds(heartbeatSeconds);

    while (completion.Task.IsCompleted == false)
    {
        Task delay = Task.Delay(heartbeatInterval);
        Task completed = await Task.WhenAny(completion.Task, delay).ConfigureAwait(continueOnCapturedContext: false);

        if (ReferenceEquals(completed, delay))
            Console.WriteLine($"Sample worker {workerName} heartbeat at {DateTimeOffset.Now:O}.");
    }

    return await completion.Task.ConfigureAwait(continueOnCapturedContext: false);
}

static Dictionary<string, string> ParseOptions(string[] args)
{
    Dictionary<string, string> options = new(StringComparer.OrdinalIgnoreCase);

    for (int index = 0; index < args.Length - 1; index += 2)
    {
        string key = args[index];

        if (key.StartsWith("--", StringComparison.Ordinal) == false)
            continue;

        options[key[2..]] = args[index + 1];
    }

    return options;
}

static string GetOption(
    IReadOnlyDictionary<string, string> options,
    string key,
    string defaultValue)
{
    return options.TryGetValue(key, out string? value)
        ? value
        : defaultValue;
}

static int GetPositiveInt32(
    IReadOnlyDictionary<string, string> options,
    string key,
    int defaultValue)
{
    if (options.TryGetValue(key, out string? configuredValue) == false)
        return defaultValue;

    if (int.TryParse(configuredValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) == false)
        return defaultValue;

    return value > 0
        ? value
        : defaultValue;
}
