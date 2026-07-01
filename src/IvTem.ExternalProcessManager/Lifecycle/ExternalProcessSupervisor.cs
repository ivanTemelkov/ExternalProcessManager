using System.Collections.Immutable;
using System.ComponentModel;
using IvTem.ExternalProcessManager.Configuration;
using IvTem.ExternalProcessManager.Scheduling;
using Microsoft.Extensions.Logging;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal sealed partial class ExternalProcessSupervisor : IExternalProcessSupervisor
{
    public ExternalProcessSupervisor(
        EffectiveExternalProcessConfiguration configuration,
        IProcessLauncher launcher,
        IProcessCleanup cleanup,
        IRestartDelay restartDelay,
        ILocalClock clock,
        IScheduledRestartTimerFactory scheduledRestartTimerFactory,
        ILogger<ExternalProcessSupervisor> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(cleanup);
        ArgumentNullException.ThrowIfNull(restartDelay);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(scheduledRestartTimerFactory);
        ArgumentNullException.ThrowIfNull(logger);

        Configuration = configuration;
        Launcher = launcher;
        Cleanup = cleanup;
        RestartDelay = restartDelay;
        Clock = clock;
        Logger = logger;
        ScheduledRestartTimer = scheduledRestartTimerFactory.Create(ExecuteScheduledRestart);
        ScheduledRestartCalculator = new ScheduledRestartCalculator(clock.TimeZone);
        BackoffState = new RestartBackoffState(configuration.Restart);
        Snapshot = CreateInitialSnapshot(configuration);
        Status = ExternalProcessStatus.NotStarted;
    }

    private EffectiveExternalProcessConfiguration Configuration { get; }

    private IProcessLauncher Launcher { get; }

    private IProcessCleanup Cleanup { get; }

    private IRestartDelay RestartDelay { get; }

    private ILocalClock Clock { get; }

    private ILogger<ExternalProcessSupervisor> Logger { get; }

    private IScheduledRestartTimer ScheduledRestartTimer { get; }

    private ScheduledRestartCalculator ScheduledRestartCalculator { get; }

    private RestartBackoffState BackoffState { get; }

    private SemaphoreSlim OperationLock { get; } = new(1, 1);

    private Lock SnapshotLock { get; } = new();

    private CancellationTokenSource LifetimeCancellation { get; } = new();

    private IProcessHandle? CurrentHandle { get; set; }

    private ExternalProcessSnapshot Snapshot { get; set; }

    private ExternalProcessStatus Status { get; set; }

    private DateTimeOffset? ScheduledRestartDueTime { get; set; }

    private int Version { get; set; }

    private bool IsDisposed { get; set; }

    public ExternalProcessSnapshot GetSnapshot()
    {
        lock (SnapshotLock)
        {
            return Snapshot with
            {
                NextScheduledRestart = GetNextScheduledRestart(),
            };
        }
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        await OperationLock.WaitAsync(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        try
        {
            ThrowIfDisposed();

            if (CurrentHandle is not null && CurrentHandle.Exited.IsCompleted == false)
                return;

            CurrentHandle?.Dispose();
            CurrentHandle = null;
            BackoffState.Reset();
            Version++;
            LogProcessStarting(Logger, Configuration.Alias);
            LaunchForCurrentVersion();
            ScheduleNextRestart();
        }
        finally
        {
            OperationLock.Release();
        }
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        await OperationLock.WaitAsync(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        try
        {
            ThrowIfDisposed();

            Version++;
            CancelScheduledRestart();

            if (CurrentHandle is null)
            {
                SetStatus(ExternalProcessStatus.Stopped);
                LogProcessStoppedWithoutHandle(Logger, Configuration.Alias);
                return;
            }

            IProcessHandle handle = CurrentHandle;
            SetStatus(ExternalProcessStatus.Stopping);
            LogProcessStopping(Logger, Configuration.Alias, handle.ProcessId);

            ProcessCleanupResult result;

            try
            {
                result = await Cleanup.Stop(
                        handle,
                        Configuration.Restart.GracefulStopTimeout,
                        cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (handle.Exited.IsCompleted == false)
                    SetRunningSnapshot(handle);

                throw;
            }

            CurrentHandle = null;
            BackoffState.Reset();
            handle.Dispose();
            ApplyStoppedResult(result);
            LogProcessStopped(Logger, Configuration.Alias, result.ProcessId, result.Outcome, result.ExitCode);
        }
        finally
        {
            OperationLock.Release();
        }
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;
        LifetimeCancellation.Cancel();
        ScheduledRestartTimer.Dispose();
        CurrentHandle?.Dispose();
        LifetimeCancellation.Dispose();
        OperationLock.Dispose();
    }

    private void LaunchForCurrentVersion()
    {
        int launchVersion = Version;

        SetStatus(ExternalProcessStatus.Starting);

        try
        {
            IProcessHandle handle = Launcher.Launch(Configuration);
            CurrentHandle = handle;
            SetRunningSnapshot(handle);
            LogProcessStarted(Logger, Configuration.Alias, handle.ProcessId);
            _ = ObserveExit(handle, launchVersion);
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or ObjectDisposedException)
        {
            CurrentHandle = null;
            SetFaultedSnapshot(exception.Message);
            LogProcessLaunchFailed(Logger, exception, Configuration.Alias);
        }
    }

    private async Task ObserveExit(IProcessHandle handle, int launchVersion)
    {
        ProcessExitResult exit;

        try
        {
            exit = await handle.Exited
                .ConfigureAwait(continueOnCapturedContext: false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await HandleObservedExit(handle, launchVersion, exit)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    private async Task HandleObservedExit(
        IProcessHandle handle,
        int launchVersion,
        ProcessExitResult exit)
    {
        TimeSpan? delay = null;
        int restartVersion = 0;

        try
        {
            await OperationLock.WaitAsync(LifetimeCancellation.Token)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            if (CurrentHandle != handle || Version != launchVersion)
                return;

            CurrentHandle = null;
            handle.Dispose();
            LogProcessExited(Logger, Configuration.Alias, exit.ExitCode);

            if (ShouldRestart(exit.ExitCode))
            {
                delay = BackoffState.GetNextDelay(handle.StartedAt, exit.ExitedAt);
                Version++;
                restartVersion = Version;
                ApplyRestartPendingResult(exit);
                LogRestartScheduled(Logger, Configuration.Alias, Configuration.Restart.Mode, delay.Value);
            }
            else
            {
                BackoffState.ResetIfStableRuntimeObserved(handle.StartedAt, exit.ExitedAt);
                ApplyExitedResult(exit, ExternalProcessStatus.Stopped);
                ScheduleNextRestart();
                LogRestartSkipped(Logger, Configuration.Alias, Configuration.Restart.Mode, exit.ExitCode);
            }
        }
        finally
        {
            OperationLock.Release();
        }

        if (delay.HasValue)
            await RestartAfterDelay(restartVersion, delay.Value)
                .ConfigureAwait(continueOnCapturedContext: false);
    }

    private async Task RestartAfterDelay(int restartVersion, TimeSpan delay)
    {
        try
        {
            await RestartDelay.Delay(delay, LifetimeCancellation.Token)
                .ConfigureAwait(continueOnCapturedContext: false);

            await OperationLock.WaitAsync(LifetimeCancellation.Token)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            if (Version != restartVersion || CurrentHandle is not null || Status != ExternalProcessStatus.RestartPending)
                return;

            LogRestartingAfterBackoff(Logger, Configuration.Alias);
            LaunchForCurrentVersion();
            ScheduleNextRestart();
        }
        finally
        {
            OperationLock.Release();
        }
    }

    private async Task ExecuteScheduledRestart()
    {
        try
        {
            await OperationLock.WaitAsync(LifetimeCancellation.Token)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            if (IsDisposed || ScheduledRestartDueTime.HasValue == false)
                return;

            if (ScheduledRestartDueTime.Value > Clock.Now)
            {
                ScheduledRestartTimer.Schedule(ScheduledRestartDueTime.Value);
                LogScheduledRestartDeferred(Logger, Configuration.Alias, ScheduledRestartDueTime.Value);
                return;
            }

            ScheduledRestartDueTime = null;
            Version++;
            LogScheduledRestartExecuting(Logger, Configuration.Alias);

            if (CurrentHandle is null || CurrentHandle.Exited.IsCompleted)
            {
                CurrentHandle?.Dispose();
                CurrentHandle = null;
                BackoffState.Reset();
                IncrementRestartCount();
                LaunchForCurrentVersion();
                ScheduleNextRestart();
                LogScheduledRestartStartedStoppedProcess(Logger, Configuration.Alias);
                return;
            }

            IProcessHandle handle = CurrentHandle;
            SetStatus(ExternalProcessStatus.Stopping);

            ProcessCleanupResult result = await Cleanup.Stop(
                    handle,
                    Configuration.Restart.GracefulStopTimeout,
                    LifetimeCancellation.Token)
                .ConfigureAwait(continueOnCapturedContext: false);

            CurrentHandle = null;
            BackoffState.Reset();
            handle.Dispose();
            ApplyScheduledRestartResult(result);
            LogScheduledRestartStoppedProcess(Logger, Configuration.Alias, result.ProcessId, result.Outcome, result.ExitCode);
            LaunchForCurrentVersion();
            ScheduleNextRestart();
        }
        catch (OperationCanceledException) when (LifetimeCancellation.IsCancellationRequested)
        {
            // Scheduled restart work is canceled when the supervisor is stopping or disposed.
        }
        finally
        {
            OperationLock.Release();
        }
    }

    private void ScheduleNextRestart()
    {
        DateTimeOffset? dueTime = GetNextScheduledRestart();
        ScheduledRestartDueTime = dueTime;

        if (dueTime.HasValue)
        {
            ScheduledRestartTimer.Schedule(dueTime.Value);
            LogScheduledRestartScheduled(Logger, Configuration.Alias, dueTime.Value);
        }
        else
        {
            ScheduledRestartTimer.Cancel();
        }
    }

    private void CancelScheduledRestart()
    {
        ScheduledRestartDueTime = null;
        ScheduledRestartTimer.Cancel();
    }

    private bool ShouldRestart(int? exitCode)
        => Configuration.Restart.Mode switch
        {
            ExternalProcessRestartMode.Always => true,
            ExternalProcessRestartMode.Never => false,
            ExternalProcessRestartMode.NonZeroExitCode => exitCode != 0,
            _ => false,
        };

    private void SetStatus(ExternalProcessStatus status)
    {
        Status = status;

        UpdateSnapshot(snapshot => snapshot with
        {
            Status = status,
        });
    }

    private void SetRunningSnapshot(IProcessHandle handle)
    {
        Status = ExternalProcessStatus.Running;

        UpdateSnapshot(snapshot => snapshot with
        {
            Status = ExternalProcessStatus.Running,
            ProcessId = handle.ProcessId,
            StartedAt = handle.StartedAt,
            ExitedAt = null,
            LastExitCode = null,
            LastError = null,
        });
    }

    private void ApplyRestartPendingResult(ProcessExitResult exit)
    {
        Status = ExternalProcessStatus.RestartPending;

        UpdateSnapshot(snapshot => snapshot with
        {
            Status = ExternalProcessStatus.RestartPending,
            ProcessId = null,
            ExitedAt = exit.ExitedAt,
            LastExitCode = exit.ExitCode,
            RestartCount = snapshot.RestartCount + 1,
            LastError = null,
        });
    }

    private void ApplyExitedResult(ProcessExitResult exit, ExternalProcessStatus status)
    {
        Status = status;

        UpdateSnapshot(snapshot => snapshot with
        {
            Status = status,
            ProcessId = null,
            ExitedAt = exit.ExitedAt,
            LastExitCode = exit.ExitCode,
            LastError = null,
        });
    }

    private void ApplyStoppedResult(ProcessCleanupResult result)
    {
        Status = ExternalProcessStatus.Stopped;

        UpdateSnapshot(snapshot => snapshot with
        {
            Status = ExternalProcessStatus.Stopped,
            ProcessId = null,
            ExitedAt = result.CompletedAt,
            LastExitCode = result.ExitCode,
            LastError = null,
        });
    }

    private void ApplyScheduledRestartResult(ProcessCleanupResult result)
    {
        Status = ExternalProcessStatus.Stopped;

        UpdateSnapshot(snapshot => snapshot with
        {
            Status = ExternalProcessStatus.Stopped,
            ProcessId = null,
            ExitedAt = result.CompletedAt,
            LastExitCode = result.ExitCode,
            RestartCount = snapshot.RestartCount + 1,
            LastError = null,
        });
    }

    private void IncrementRestartCount()
    {
        UpdateSnapshot(snapshot => snapshot with
        {
            RestartCount = snapshot.RestartCount + 1,
        });
    }

    private void SetFaultedSnapshot(string error)
    {
        Status = ExternalProcessStatus.Faulted;

        UpdateSnapshot(snapshot => snapshot with
        {
            Status = ExternalProcessStatus.Faulted,
            ProcessId = null,
            LastError = error,
        });
    }

    private void UpdateSnapshot(Func<ExternalProcessSnapshot, ExternalProcessSnapshot> update)
    {
        lock (SnapshotLock)
        {
            Snapshot = update(Snapshot) with
            {
                NextScheduledRestart = GetNextScheduledRestart(),
            };
        }
    }

    private DateTimeOffset? GetNextScheduledRestart()
        => ScheduledRestartCalculator.GetNextOccurrence(Clock.Now, Configuration.ScheduledRestarts);

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
    }

    private static ExternalProcessSnapshot CreateInitialSnapshot(EffectiveExternalProcessConfiguration configuration)
        => new()
        {
            Alias = configuration.Alias,
            Status = ExternalProcessStatus.NotStarted,
            FileName = configuration.FileName,
            Arguments = CreateArgumentSnapshot(configuration),
            WorkingDirectory = configuration.WorkingDirectory,
        };

    private static ImmutableArray<string> CreateArgumentSnapshot(EffectiveExternalProcessConfiguration configuration)
        => configuration.ArgumentMode switch
        {
            EffectiveProcessArgumentMode.ArgumentList => configuration.ArgumentList,
            EffectiveProcessArgumentMode.RawString when string.IsNullOrWhiteSpace(configuration.Arguments) == false => [configuration.Arguments],
            _ => [],
        };

    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "Starting external process {Alias}.")]
    private static partial void LogProcessStarting(ILogger logger, string alias);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "External process {Alias} started with process ID {ProcessId}.")]
    private static partial void LogProcessStarted(ILogger logger, string alias, int processId);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Error, Message = "External process {Alias} failed to start.")]
    private static partial void LogProcessLaunchFailed(ILogger logger, Exception exception, string alias);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Stopping external process {Alias} with process ID {ProcessId}.")]
    private static partial void LogProcessStopping(ILogger logger, string alias, int processId);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "External process {Alias} stopped with process ID {ProcessId}, cleanup outcome {Outcome}, and exit code {ExitCode}.")]
    private static partial void LogProcessStopped(ILogger logger, string alias, int processId, ProcessCleanupOutcome outcome, int? exitCode);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "External process {Alias} stop requested with no current process handle.")]
    private static partial void LogProcessStoppedWithoutHandle(ILogger logger, string alias);

    [LoggerMessage(EventId = 2006, Level = LogLevel.Information, Message = "External process {Alias} exited with exit code {ExitCode}.")]
    private static partial void LogProcessExited(ILogger logger, string alias, int? exitCode);

    [LoggerMessage(EventId = 2007, Level = LogLevel.Information, Message = "External process {Alias} restart scheduled by mode {RestartMode} after delay {Delay}.")]
    private static partial void LogRestartScheduled(ILogger logger, string alias, ExternalProcessRestartMode restartMode, TimeSpan delay);

    [LoggerMessage(EventId = 2008, Level = LogLevel.Information, Message = "External process {Alias} restart skipped by mode {RestartMode} after exit code {ExitCode}.")]
    private static partial void LogRestartSkipped(ILogger logger, string alias, ExternalProcessRestartMode restartMode, int? exitCode);

    [LoggerMessage(EventId = 2009, Level = LogLevel.Information, Message = "External process {Alias} is restarting after backoff delay.")]
    private static partial void LogRestartingAfterBackoff(ILogger logger, string alias);

    [LoggerMessage(EventId = 2010, Level = LogLevel.Information, Message = "Scheduled restart for external process {Alias} is executing.")]
    private static partial void LogScheduledRestartExecuting(ILogger logger, string alias);

    [LoggerMessage(EventId = 2011, Level = LogLevel.Information, Message = "Scheduled restart for external process {Alias} stopped process ID {ProcessId} with cleanup outcome {Outcome} and exit code {ExitCode}.")]
    private static partial void LogScheduledRestartStoppedProcess(ILogger logger, string alias, int processId, ProcessCleanupOutcome outcome, int? exitCode);

    [LoggerMessage(EventId = 2012, Level = LogLevel.Information, Message = "Scheduled restart for external process {Alias} started a stopped process.")]
    private static partial void LogScheduledRestartStartedStoppedProcess(ILogger logger, string alias);

    [LoggerMessage(EventId = 2013, Level = LogLevel.Information, Message = "Scheduled restart for external process {Alias} scheduled at {DueTime}.")]
    private static partial void LogScheduledRestartScheduled(ILogger logger, string alias, DateTimeOffset dueTime);

    [LoggerMessage(EventId = 2014, Level = LogLevel.Information, Message = "Scheduled restart for external process {Alias} deferred until {DueTime}.")]
    private static partial void LogScheduledRestartDeferred(ILogger logger, string alias, DateTimeOffset dueTime);
}
