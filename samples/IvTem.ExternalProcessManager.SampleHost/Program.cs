using System.Globalization;
using IvTem.ExternalProcessManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

if (OperatingSystem.IsWindows() == false)
{
    Console.Error.WriteLine("IvTem.ExternalProcessManager samples require Windows process-control APIs.");
    return 1;
}

HostApplicationBuilderSettings settings = new()
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
};

HostApplicationBuilder builder = Host.CreateApplicationBuilder(settings);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

string workerPath = SampleWorkerPath.Resolve();
string workerWorkingDirectory = Path.GetDirectoryName(workerPath) is string directory
    ? directory
    : AppContext.BaseDirectory;

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
{
    ["ExternalProcessManager:Processes:0:FileName"] = workerPath,
    ["ExternalProcessManager:Processes:0:WorkingDirectory"] = workerWorkingDirectory,
    ["ExternalProcessManager:Processes:0:ArgumentList:0"] = "--name",
    ["ExternalProcessManager:Processes:0:ArgumentList:1"] = "sample-worker",
    ["ExternalProcessManager:Processes:0:ArgumentList:2"] = "--heartbeat-seconds",
    ["ExternalProcessManager:Processes:0:ArgumentList:3"] = "1",
});

builder.Services.AddExternalProcessManager(
    builder.Configuration.GetSection("ExternalProcessManager"));
builder.Services.AddHostedService<SampleDiagnosticsService>();

using IHost host = builder.Build();
await host.RunAsync().ConfigureAwait(continueOnCapturedContext: false);

return 0;

internal static class SampleWorkerPath
{
    private const string WorkerProjectName = "IvTem.ExternalProcessManager.SampleWorker";
    private const string WorkerExecutableName = $"{WorkerProjectName}.exe";

    public static string Resolve()
    {
        string publishedWorkerPath = Path.Combine(AppContext.BaseDirectory, WorkerExecutableName);

        if (IsPublishDirectory()
            && File.Exists(publishedWorkerPath))
        {
            return publishedWorkerPath;
        }

        string workerPath = ResolveBuildOutputPath();

        if (File.Exists(workerPath) == false)
            throw new FileNotFoundException("Build or publish the sample worker before running the sample host.", workerPath);

        return workerPath;
    }

    private static string ResolveBuildOutputPath()
    {
        DirectoryInfo samplesDirectory = FindSamplesDirectory();
        DirectoryInfo outputDirectory = new(AppContext.BaseDirectory);
        string configurationName = FindConfigurationName(outputDirectory);
        string workerOutputDirectory = Path.Combine(
            samplesDirectory.FullName,
            WorkerProjectName,
            "bin",
            configurationName,
            "net10.0");
        string runtimeOutputDirectory = Path.Combine(workerOutputDirectory, "win-x64");

        return File.Exists(Path.Combine(runtimeOutputDirectory, WorkerExecutableName))
            ? Path.Combine(runtimeOutputDirectory, WorkerExecutableName)
            : Path.Combine(workerOutputDirectory, WorkerExecutableName);
    }

    private static DirectoryInfo FindSamplesDirectory()
    {
        DirectoryInfo? currentDirectory = new(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            string workerProjectDirectory = Path.Combine(currentDirectory.FullName, WorkerProjectName);

            if (Directory.Exists(workerProjectDirectory))
                return currentDirectory;

            currentDirectory = currentDirectory.Parent;
        }

        throw new DirectoryNotFoundException("The samples directory could not be found from the sample host output path.");
    }

    private static string FindConfigurationName(DirectoryInfo outputDirectory)
    {
        DirectoryInfo? currentDirectory = outputDirectory;

        while (currentDirectory is not null)
        {
            if (currentDirectory.Name.Equals("Debug", StringComparison.OrdinalIgnoreCase)
                || currentDirectory.Name.Equals("Release", StringComparison.OrdinalIgnoreCase))
            {
                return currentDirectory.Name;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return "Debug";
    }

    private static bool IsPublishDirectory()
    {
        DirectoryInfo outputDirectory = new(AppContext.BaseDirectory);
        return outputDirectory.Name.Equals("publish", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class SampleDiagnosticsService(
    IExternalProcessManager manager,
    IConfiguration configuration,
    IHostApplicationLifetime lifetime,
    ILogger<SampleDiagnosticsService> logger)
    : BackgroundService
{
    private static readonly TimeSpan SnapshotInterval = TimeSpan.FromSeconds(2);

    private IExternalProcessManager Manager { get; } = manager;

    private IConfiguration Configuration { get; } = configuration;

    private IHostApplicationLifetime Lifetime { get; } = lifetime;

    private ILogger<SampleDiagnosticsService> Logger { get; } = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan runDuration = GetRunDuration();
        DateTimeOffset? stopAt = runDuration > TimeSpan.Zero
            ? DateTimeOffset.UtcNow.Add(runDuration)
            : null;

        try
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                LogSnapshot();

                if (stopAt.HasValue
                    && DateTimeOffset.UtcNow >= stopAt.Value)
                {
                    Logger.LogInformation("Sample run duration elapsed after {RunSeconds} seconds.", runDuration.TotalSeconds);
                    Lifetime.StopApplication();
                    return;
                }

                TimeSpan delay = GetNextDelay(stopAt);
                await Task.Delay(delay, stoppingToken).ConfigureAwait(continueOnCapturedContext: false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private TimeSpan GetRunDuration()
    {
        string? configuredRunSeconds = Configuration["SampleHost:RunSeconds"];

        if (string.IsNullOrWhiteSpace(configuredRunSeconds))
            return TimeSpan.Zero;

        if (int.TryParse(
            configuredRunSeconds,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int runSeconds) == false)
        {
            Logger.LogWarning("Ignoring invalid SampleHost:RunSeconds value {RunSeconds}.", configuredRunSeconds);
            return TimeSpan.Zero;
        }

        return runSeconds > 0
            ? TimeSpan.FromSeconds(runSeconds)
            : TimeSpan.Zero;
    }

    private static TimeSpan GetNextDelay(DateTimeOffset? stopAt)
    {
        if (stopAt.HasValue == false)
            return SnapshotInterval;

        TimeSpan remaining = stopAt.Value - DateTimeOffset.UtcNow;

        if (remaining <= TimeSpan.Zero)
            return TimeSpan.FromMilliseconds(1);

        return remaining < SnapshotInterval
            ? remaining
            : SnapshotInterval;
    }

    private void LogSnapshot()
    {
        ExternalProcessManagerSnapshot snapshot = Manager.GetSnapshot();

        Logger.LogInformation(
            "External process manager running: {IsRunning}; generated at {GeneratedAt}; processes: {ProcessCount}; validation errors: {ValidationErrorCount}.",
            snapshot.IsRunning,
            snapshot.GeneratedAt,
            snapshot.Processes.Length,
            snapshot.ValidationErrors.Length);

        foreach (ExternalProcessSnapshot process in snapshot.Processes)
        {
            Logger.LogInformation(
                "Managed process {Alias}: {Status}; pid {ProcessId}; restarts {RestartCount}; next scheduled restart {NextScheduledRestart}; validation errors {ValidationErrorCount}.",
                process.Alias,
                process.Status,
                process.ProcessId,
                process.RestartCount,
                process.NextScheduledRestart,
                process.ValidationErrors.Length);
        }

        foreach (ExternalProcessValidationError validationError in snapshot.ValidationErrors)
        {
            Logger.LogWarning(
                "Validation error for alias {Alias} at {Path}: {Message}",
                validationError.Alias,
                validationError.Path,
                validationError.Message);
        }
    }
}
