using System.Globalization;
using IvTem.ExternalProcessManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IvTem.ExternalProcessManager.SampleHost;

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
            // Host shutdown cancels the sample diagnostics loop.
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
