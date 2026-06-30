using System.Collections.Immutable;
using IvTem.ExternalProcessManager.Configuration;
using IvTem.ExternalProcessManager.Lifecycle;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace IvTem.ExternalProcessManager;

internal sealed partial class ExternalProcessManager : IExternalProcessManager, IDisposable
{
    public ExternalProcessManager(
        ExternalProcessManagerConfigurationSource configurationSource,
        ExternalProcessConfigurationReader configurationReader,
        ExternalProcessConfigurationValidator configurationValidator,
        IExternalProcessSupervisorFactory supervisorFactory,
        ILogger<ExternalProcessManager> logger)
    {
        ArgumentNullException.ThrowIfNull(configurationSource);
        ArgumentNullException.ThrowIfNull(configurationReader);
        ArgumentNullException.ThrowIfNull(configurationValidator);
        ArgumentNullException.ThrowIfNull(supervisorFactory);
        ArgumentNullException.ThrowIfNull(logger);

        ConfigurationSource = configurationSource;
        ConfigurationReader = configurationReader;
        ConfigurationValidator = configurationValidator;
        SupervisorFactory = supervisorFactory;
        Logger = logger;
        Snapshot = CreateSnapshot(isRunning: false, processes: [], validationErrors: []);
    }

    private ExternalProcessManagerConfigurationSource ConfigurationSource { get; }

    private ExternalProcessConfigurationReader ConfigurationReader { get; }

    private ExternalProcessConfigurationValidator ConfigurationValidator { get; }

    private IExternalProcessSupervisorFactory SupervisorFactory { get; }

    private ILogger<ExternalProcessManager> Logger { get; }

    private SemaphoreSlim ReconciliationLock { get; } = new(1, 1);

    private Lock SnapshotLock { get; } = new();

    private Dictionary<string, ManagedProcessEntry> Supervisors { get; } = new(StringComparer.OrdinalIgnoreCase);

    private ExternalProcessManagerSnapshot Snapshot { get; set; }

    private ImmutableArray<InvalidExternalProcessConfiguration> InvalidProcesses { get; set; } = [];

    private ImmutableArray<ExternalProcessValidationError> ValidationErrors { get; set; } = [];

    private IDisposable? ConfigurationChangeRegistration { get; set; }

    private bool IsRunning { get; set; }

    private bool IsDisposed { get; set; }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await ReconciliationLock.WaitAsync(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        try
        {
            ThrowIfDisposed();

            if (IsRunning)
                return;

            IsRunning = true;
            SubscribeToConfigurationChanges();
            LogManagerStarting(Logger);

            EffectiveExternalProcessManagerConfiguration configuration = ReadConfiguration();

            await ApplyConfiguration(configuration, startExistingSupervisors: true, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            LogManagerStarted(Logger, configuration.Processes.Length, configuration.ValidationErrors.Length);
        }
        finally
        {
            ReconciliationLock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await ReconciliationLock.WaitAsync(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        try
        {
            ThrowIfDisposed();

            if (IsRunning == false)
            {
                RefreshSnapshot();
                return;
            }

            IsRunning = false;
            ConfigurationChangeRegistration?.Dispose();
            ConfigurationChangeRegistration = null;
            LogManagerStopping(Logger, Supervisors.Count);

            foreach (ManagedProcessEntry entry in GetSupervisorEntries())
            {
                await entry.Supervisor.Stop(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                RefreshSnapshot();
            }

            RefreshSnapshot();
            LogManagerStopped(Logger);
        }
        finally
        {
            ReconciliationLock.Release();
        }
    }

    public ExternalProcessManagerSnapshot GetSnapshot()
    {
        lock (SnapshotLock)
        {
            RefreshSnapshotCore();
            return Snapshot;
        }
    }

    public void Dispose()
        => Dispose(disposingAsync: false)
            .AsTask()
            .GetAwaiter()
            .GetResult();

    public async ValueTask DisposeAsync()
        => await Dispose(disposingAsync: true)
            .ConfigureAwait(continueOnCapturedContext: false);

    private async ValueTask Dispose(bool disposingAsync)
    {
        if (IsDisposed)
            return;

        ImmutableArray<ManagedProcessEntry> entries;

        await ReconciliationLock.WaitAsync()
            .ConfigureAwait(continueOnCapturedContext: false);

        try
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            IsRunning = false;
            ConfigurationChangeRegistration?.Dispose();
            ConfigurationChangeRegistration = null;
            entries = GetSupervisorEntries();
        }
        finally
        {
            ReconciliationLock.Release();
        }

        await StopAndDisposeEntries(entries, disposingAsync)
            .ConfigureAwait(continueOnCapturedContext: false);

        lock (SnapshotLock)
        {
            Supervisors.Clear();
            RefreshSnapshotCore();
        }

        ReconciliationLock.Dispose();
    }

    private static async ValueTask StopAndDisposeEntries(
        ImmutableArray<ManagedProcessEntry> entries,
        bool disposingAsync)
    {
        foreach (ManagedProcessEntry entry in entries)
        {
            if (disposingAsync)
            {
                await entry.Supervisor.Stop(CancellationToken.None)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
            else
            {
                entry.Supervisor.Stop(CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }

            entry.Supervisor.Dispose();
        }
    }

    private void SubscribeToConfigurationChanges()
    {
        ConfigurationChangeRegistration?.Dispose();
        ConfigurationChangeRegistration = ChangeToken.OnChange(
            () => ConfigurationSource.Section.GetReloadToken(),
            OnConfigurationChanged);
    }

    private void OnConfigurationChanged()
        => _ = ReconcileChangedConfiguration();

    private async Task ReconcileChangedConfiguration()
    {
        try
        {
            await ReconciliationLock.WaitAsync()
                .ConfigureAwait(continueOnCapturedContext: false);
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        try
        {
            if (IsDisposed || IsRunning == false)
                return;

            LogConfigurationReloadDetected(Logger);
            EffectiveExternalProcessManagerConfiguration configuration = ReadConfiguration();

            await ApplyConfiguration(configuration, startExistingSupervisors: false, CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            LogConfigurationReloadApplied(Logger, configuration.Processes.Length, configuration.ValidationErrors.Length);
        }
        finally
        {
            ReconciliationLock.Release();
        }
    }

    private EffectiveExternalProcessManagerConfiguration ReadConfiguration()
    {
        EffectiveExternalProcessManagerConfiguration configuration =
            ConfigurationValidator.Validate(ConfigurationReader.Read(ConfigurationSource.Section));

        LogValidationErrors(configuration.ValidationErrors);
        return configuration;
    }

    private async Task ApplyConfiguration(
        EffectiveExternalProcessManagerConfiguration configuration,
        bool startExistingSupervisors,
        CancellationToken cancellationToken)
    {
        Dictionary<string, EffectiveExternalProcessConfiguration> validProcesses = BuildValidProcessMap(configuration);
        HashSet<string> invalidAliases = BuildInvalidAliasSet(configuration.InvalidProcesses);

        foreach (EffectiveExternalProcessConfiguration process in configuration.Processes)
        {
            await ApplyValidProcess(process, startExistingSupervisors, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        List<string> removedAliases = GetRemovedAliases(validProcesses, invalidAliases);

        foreach (string alias in removedAliases)
        {
            await RemoveProcess(alias, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        PublishSnapshot(configuration.InvalidProcesses, configuration.ValidationErrors);
    }

    private async Task ApplyValidProcess(
        EffectiveExternalProcessConfiguration configuration,
        bool startExistingSupervisor,
        CancellationToken cancellationToken)
    {
        ManagedProcessEntry? existingEntry = GetSupervisorEntry(configuration.AliasKey);

        if (existingEntry is not null)
        {
            if (AreEquivalent(existingEntry.Configuration, configuration))
            {
                if (startExistingSupervisor)
                {
                    LogStartingExistingProcess(Logger, configuration.Alias);
                    await existingEntry.Supervisor.Start(cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false);
                }

                return;
            }

            LogReplacingProcess(Logger, configuration.Alias);
            await existingEntry.Supervisor.Stop(cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            existingEntry.Supervisor.Dispose();
        }

        IExternalProcessSupervisor supervisor = SupervisorFactory.Create(configuration);
        LogAddingProcess(Logger, configuration.Alias);

        lock (SnapshotLock)
        {
            Supervisors[configuration.AliasKey] = new ManagedProcessEntry(configuration, supervisor);
            RefreshSnapshotCore();
        }

        await supervisor.Start(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    private async Task RemoveProcess(string alias, CancellationToken cancellationToken)
    {
        ManagedProcessEntry? entry;

        lock (SnapshotLock)
        {
            if (Supervisors.TryGetValue(alias, out entry) == false)
                return;
        }

        LogRemovingProcess(Logger, alias);
        await entry.Supervisor.Stop(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        lock (SnapshotLock)
        {
            Supervisors.Remove(alias);
            RefreshSnapshotCore();
        }

        entry.Supervisor.Dispose();
    }

    private ManagedProcessEntry? GetSupervisorEntry(string alias)
    {
        lock (SnapshotLock)
        {
            Supervisors.TryGetValue(alias, out ManagedProcessEntry? entry);
            return entry;
        }
    }

    private ImmutableArray<ManagedProcessEntry> GetSupervisorEntries()
    {
        lock (SnapshotLock)
        {
            return [.. Supervisors.Values];
        }
    }

    private List<string> GetRemovedAliases(
        Dictionary<string, EffectiveExternalProcessConfiguration> validProcesses,
        HashSet<string> invalidAliases)
    {
        lock (SnapshotLock)
        {
            return
            [
                .. Supervisors.Keys.Where(alias =>
                    validProcesses.ContainsKey(alias) == false
                    && invalidAliases.Contains(alias) == false),
            ];
        }
    }

    private void PublishSnapshot(
        ImmutableArray<InvalidExternalProcessConfiguration> invalidProcesses,
        ImmutableArray<ExternalProcessValidationError> validationErrors)
    {
        lock (SnapshotLock)
        {
            InvalidProcesses = invalidProcesses;
            ValidationErrors = validationErrors;
            RefreshSnapshotCore();
        }
    }

    private void RefreshSnapshot()
    {
        lock (SnapshotLock)
        {
            RefreshSnapshotCore();
        }
    }

    private void RefreshSnapshotCore()
    {
        Dictionary<string, ImmutableArray<ExternalProcessValidationError>> validationErrorsByAlias =
            BuildValidationErrorMap(InvalidProcesses);
        List<ExternalProcessSnapshot> processSnapshots = [];

        foreach (KeyValuePair<string, ManagedProcessEntry> item in Supervisors.OrderBy(
            item => item.Key,
            StringComparer.OrdinalIgnoreCase))
        {
            ExternalProcessSnapshot snapshot = item.Value.Supervisor.GetSnapshot();

            if (validationErrorsByAlias.TryGetValue(snapshot.Alias, out ImmutableArray<ExternalProcessValidationError> errors))
            {
                snapshot = snapshot with
                {
                    LastError = errors[0].Message,
                    ValidationErrors = errors,
                };
            }

            processSnapshots.Add(snapshot);
        }

        foreach (InvalidExternalProcessConfiguration invalidProcess in InvalidProcesses)
        {
            if (invalidProcess.Alias is not null && Supervisors.ContainsKey(invalidProcess.Alias))
                continue;

            processSnapshots.Add(CreateInvalidSnapshot(invalidProcess));
        }

        Snapshot = CreateSnapshot(IsRunning, [.. processSnapshots], ValidationErrors);
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(IsDisposed, this);

    private static Dictionary<string, EffectiveExternalProcessConfiguration> BuildValidProcessMap(
        EffectiveExternalProcessManagerConfiguration configuration)
    {
        Dictionary<string, EffectiveExternalProcessConfiguration> validProcesses = new(StringComparer.OrdinalIgnoreCase);

        foreach (EffectiveExternalProcessConfiguration process in configuration.Processes)
        {
            validProcesses[process.AliasKey] = process;
        }

        return validProcesses;
    }

    private static HashSet<string> BuildInvalidAliasSet(
        ImmutableArray<InvalidExternalProcessConfiguration> invalidProcesses)
    {
        HashSet<string> invalidAliases = new(StringComparer.OrdinalIgnoreCase);

        foreach (InvalidExternalProcessConfiguration invalidProcess in invalidProcesses)
        {
            if (string.IsNullOrWhiteSpace(invalidProcess.Alias) == false)
                invalidAliases.Add(invalidProcess.Alias);
        }

        return invalidAliases;
    }

    private static Dictionary<string, ImmutableArray<ExternalProcessValidationError>> BuildValidationErrorMap(
        ImmutableArray<InvalidExternalProcessConfiguration> invalidProcesses)
    {
        Dictionary<string, ImmutableArray<ExternalProcessValidationError>.Builder> builders = new(StringComparer.OrdinalIgnoreCase);

        foreach (InvalidExternalProcessConfiguration invalidProcess in invalidProcesses)
        {
            if (invalidProcess.Alias is null)
                continue;

            if (builders.TryGetValue(invalidProcess.Alias, out ImmutableArray<ExternalProcessValidationError>.Builder? builder) == false)
            {
                builder = ImmutableArray.CreateBuilder<ExternalProcessValidationError>();
                builders.Add(invalidProcess.Alias, builder);
            }

            builder.AddRange(invalidProcess.ValidationErrors);
        }

        return builders.ToDictionary(
            item => item.Key,
            item => item.Value.ToImmutable(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static ExternalProcessSnapshot CreateInvalidSnapshot(InvalidExternalProcessConfiguration invalidProcess)
        => new()
        {
            Alias = invalidProcess.Alias ?? invalidProcess.Path,
            Status = ExternalProcessStatus.InvalidConfiguration,
            LastError = invalidProcess.ValidationErrors.IsEmpty ? null : invalidProcess.ValidationErrors[0].Message,
            ValidationErrors = invalidProcess.ValidationErrors,
        };

    private static ExternalProcessManagerSnapshot CreateSnapshot(
        bool isRunning,
        ImmutableArray<ExternalProcessSnapshot> processes,
        ImmutableArray<ExternalProcessValidationError> validationErrors)
        => new()
        {
            IsRunning = isRunning,
            GeneratedAt = DateTimeOffset.UtcNow,
            Processes = processes,
            ValidationErrors = validationErrors,
        };

    private static bool AreEquivalent(
        EffectiveExternalProcessConfiguration left,
        EffectiveExternalProcessConfiguration right)
        => string.Equals(left.FileName, right.FileName, StringComparison.Ordinal)
            && left.ArgumentMode == right.ArgumentMode
            && string.Equals(left.Arguments, right.Arguments, StringComparison.Ordinal)
            && left.ArgumentList.SequenceEqual(right.ArgumentList, StringComparer.Ordinal)
            && string.Equals(left.WorkingDirectory, right.WorkingDirectory, StringComparison.Ordinal)
            && AreEquivalent(left.Environment, right.Environment)
            && left.Restart == right.Restart
            && AreEquivalent(left.ScheduledRestarts, right.ScheduledRestarts);

    private static bool AreEquivalent(
        ImmutableDictionary<string, string> left,
        ImmutableDictionary<string, string> right)
    {
        if (left.Count != right.Count)
            return false;

        foreach (KeyValuePair<string, string> item in left)
        {
            if (right.TryGetValue(item.Key, out string? value) == false)
                return false;

            if (string.Equals(item.Value, value, StringComparison.Ordinal) == false)
                return false;
        }

        return true;
    }

    private static bool AreEquivalent(
        ImmutableArray<EffectiveScheduledRestartConfiguration> left,
        ImmutableArray<EffectiveScheduledRestartConfiguration> right)
    {
        if (left.Length != right.Length)
            return false;

        for (int i = 0; i < left.Length; i++)
        {
            if (left[i].HourOfDay != right[i].HourOfDay)
                return false;

            if (left[i].Days.SequenceEqual(right[i].Days) == false)
                return false;
        }

        return true;
    }

    private void LogValidationErrors(ImmutableArray<ExternalProcessValidationError> validationErrors)
    {
        foreach (ExternalProcessValidationError validationError in validationErrors)
        {
            LogValidationError(
                Logger,
                validationError.Path,
                validationError.Message);
        }
    }

    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "External process manager is starting.")]
    private static partial void LogManagerStarting(ILogger logger);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "External process manager started with {ProcessCount} valid process definitions and {ValidationErrorCount} validation errors.")]
    private static partial void LogManagerStarted(ILogger logger, int processCount, int validationErrorCount);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "External process manager is stopping {ProcessCount} supervised processes.")]
    private static partial void LogManagerStopping(ILogger logger, int processCount);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "External process manager stopped.")]
    private static partial void LogManagerStopped(ILogger logger);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Information, Message = "External process manager configuration reload detected.")]
    private static partial void LogConfigurationReloadDetected(ILogger logger);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "External process manager configuration reload applied with {ProcessCount} valid process definitions and {ValidationErrorCount} validation errors.")]
    private static partial void LogConfigurationReloadApplied(ILogger logger, int processCount, int validationErrorCount);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Warning, Message = "External process configuration validation error at {Path}: {Message}")]
    private static partial void LogValidationError(ILogger logger, string path, string message);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Information, Message = "Starting existing external process supervisor for alias {Alias}.")]
    private static partial void LogStartingExistingProcess(ILogger logger, string alias);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Information, Message = "Replacing external process supervisor for alias {Alias}.")]
    private static partial void LogReplacingProcess(ILogger logger, string alias);

    [LoggerMessage(EventId = 1009, Level = LogLevel.Information, Message = "Adding external process supervisor for alias {Alias}.")]
    private static partial void LogAddingProcess(ILogger logger, string alias);

    [LoggerMessage(EventId = 1010, Level = LogLevel.Information, Message = "Removing external process supervisor for alias {Alias}.")]
    private static partial void LogRemovingProcess(ILogger logger, string alias);

    private sealed record ManagedProcessEntry(
        EffectiveExternalProcessConfiguration Configuration,
        IExternalProcessSupervisor Supervisor);
}
