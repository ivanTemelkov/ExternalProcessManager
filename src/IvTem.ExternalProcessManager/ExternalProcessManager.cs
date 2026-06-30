using System.Collections.Immutable;
using IvTem.ExternalProcessManager.Configuration;
using IvTem.ExternalProcessManager.Lifecycle;
using Microsoft.Extensions.Primitives;

namespace IvTem.ExternalProcessManager;

internal sealed class ExternalProcessManager : IExternalProcessManager, IDisposable
{
    public ExternalProcessManager(
        ExternalProcessManagerConfigurationSource configurationSource,
        ExternalProcessConfigurationReader configurationReader,
        ExternalProcessConfigurationValidator configurationValidator,
        IExternalProcessSupervisorFactory supervisorFactory)
    {
        ArgumentNullException.ThrowIfNull(configurationSource);
        ArgumentNullException.ThrowIfNull(configurationReader);
        ArgumentNullException.ThrowIfNull(configurationValidator);
        ArgumentNullException.ThrowIfNull(supervisorFactory);

        ConfigurationSource = configurationSource;
        ConfigurationReader = configurationReader;
        ConfigurationValidator = configurationValidator;
        SupervisorFactory = supervisorFactory;
        Snapshot = CreateSnapshot(isRunning: false, processes: [], validationErrors: []);
    }

    private ExternalProcessManagerConfigurationSource ConfigurationSource { get; }

    private ExternalProcessConfigurationReader ConfigurationReader { get; }

    private ExternalProcessConfigurationValidator ConfigurationValidator { get; }

    private IExternalProcessSupervisorFactory SupervisorFactory { get; }

    private SemaphoreSlim ReconciliationLock { get; } = new(1, 1);

    private Lock SnapshotLock { get; } = new();

    private Dictionary<string, ManagedProcessEntry> Supervisors { get; } = new(StringComparer.OrdinalIgnoreCase);

    private ExternalProcessManagerSnapshot Snapshot { get; set; }

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

            EffectiveExternalProcessManagerConfiguration configuration = ReadConfiguration();

            await ApplyConfiguration(configuration, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
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
                PublishSnapshot(invalidProcesses: [], validationErrors: []);
                return;
            }

            IsRunning = false;
            ConfigurationChangeRegistration?.Dispose();
            ConfigurationChangeRegistration = null;

            foreach (ManagedProcessEntry entry in Supervisors.Values)
            {
                await entry.Supervisor.Stop(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                entry.Supervisor.Dispose();
            }

            Supervisors.Clear();
            PublishSnapshot(invalidProcesses: [], validationErrors: []);
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
            return Snapshot;
        }
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;
        ConfigurationChangeRegistration?.Dispose();

        foreach (ManagedProcessEntry entry in Supervisors.Values)
        {
            entry.Supervisor.Dispose();
        }

        Supervisors.Clear();
        ReconciliationLock.Dispose();
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

            EffectiveExternalProcessManagerConfiguration configuration = ReadConfiguration();

            await ApplyConfiguration(configuration, CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
        finally
        {
            ReconciliationLock.Release();
        }
    }

    private EffectiveExternalProcessManagerConfiguration ReadConfiguration()
        => ConfigurationValidator.Validate(ConfigurationReader.Read(ConfigurationSource.Section));

    private async Task ApplyConfiguration(
        EffectiveExternalProcessManagerConfiguration configuration,
        CancellationToken cancellationToken)
    {
        Dictionary<string, EffectiveExternalProcessConfiguration> validProcesses = BuildValidProcessMap(configuration);
        HashSet<string> invalidAliases = BuildInvalidAliasSet(configuration.InvalidProcesses);

        foreach (EffectiveExternalProcessConfiguration process in configuration.Processes)
        {
            await ApplyValidProcess(process, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        List<string> removedAliases =
        [
            .. Supervisors.Keys.Where(alias =>
                validProcesses.ContainsKey(alias) == false
                && invalidAliases.Contains(alias) == false),
        ];

        foreach (string alias in removedAliases)
        {
            await RemoveProcess(alias, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        PublishSnapshot(configuration.InvalidProcesses, configuration.ValidationErrors);
    }

    private async Task ApplyValidProcess(
        EffectiveExternalProcessConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (Supervisors.TryGetValue(configuration.AliasKey, out ManagedProcessEntry? existingEntry))
        {
            if (AreEquivalent(existingEntry.Configuration, configuration))
                return;

            await existingEntry.Supervisor.Stop(cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            existingEntry.Supervisor.Dispose();
        }

        IExternalProcessSupervisor supervisor = SupervisorFactory.Create(configuration);
        Supervisors[configuration.AliasKey] = new ManagedProcessEntry(configuration, supervisor);

        await supervisor.Start(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    private async Task RemoveProcess(string alias, CancellationToken cancellationToken)
    {
        if (Supervisors.Remove(alias, out ManagedProcessEntry? entry) == false)
            return;

        await entry.Supervisor.Stop(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        entry.Supervisor.Dispose();
    }

    private void PublishSnapshot(
        ImmutableArray<InvalidExternalProcessConfiguration> invalidProcesses,
        ImmutableArray<ExternalProcessValidationError> validationErrors)
    {
        Dictionary<string, ImmutableArray<ExternalProcessValidationError>> validationErrorsByAlias =
            BuildValidationErrorMap(invalidProcesses);
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

        foreach (InvalidExternalProcessConfiguration invalidProcess in invalidProcesses)
        {
            if (invalidProcess.Alias is not null && Supervisors.ContainsKey(invalidProcess.Alias))
                continue;

            processSnapshots.Add(CreateInvalidSnapshot(invalidProcess));
        }

        lock (SnapshotLock)
        {
            Snapshot = CreateSnapshot(IsRunning, [.. processSnapshots], validationErrors);
        }
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

    private sealed record ManagedProcessEntry(
        EffectiveExternalProcessConfiguration Configuration,
        IExternalProcessSupervisor Supervisor);
}
