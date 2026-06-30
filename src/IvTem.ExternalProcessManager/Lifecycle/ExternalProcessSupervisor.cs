using System.Collections.Immutable;
using System.ComponentModel;
using IvTem.ExternalProcessManager.Configuration;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal sealed class ExternalProcessSupervisor : IDisposable
{
    public ExternalProcessSupervisor(
        EffectiveExternalProcessConfiguration configuration,
        IProcessLauncher launcher,
        IProcessCleanup cleanup,
        IRestartDelay restartDelay)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(cleanup);
        ArgumentNullException.ThrowIfNull(restartDelay);

        Configuration = configuration;
        Launcher = launcher;
        Cleanup = cleanup;
        RestartDelay = restartDelay;
        BackoffState = new RestartBackoffState(configuration.Restart);
        Snapshot = CreateInitialSnapshot(configuration);
        Status = ExternalProcessStatus.NotStarted;
    }

    private EffectiveExternalProcessConfiguration Configuration { get; }

    private IProcessLauncher Launcher { get; }

    private IProcessCleanup Cleanup { get; }

    private IRestartDelay RestartDelay { get; }

    private RestartBackoffState BackoffState { get; }

    private SemaphoreSlim OperationLock { get; } = new(1, 1);

    private Lock SnapshotLock { get; } = new();

    private CancellationTokenSource LifetimeCancellation { get; } = new();

    private IProcessHandle? CurrentHandle { get; set; }

    private ExternalProcessSnapshot Snapshot { get; set; }

    private ExternalProcessStatus Status { get; set; }

    private int Version { get; set; }

    private bool IsDisposed { get; set; }

    public ExternalProcessSnapshot GetSnapshot()
    {
        lock (SnapshotLock)
        {
            return Snapshot;
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
            LaunchForCurrentVersion();
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

            if (CurrentHandle is null)
            {
                SetStatus(ExternalProcessStatus.Stopped);
                return;
            }

            IProcessHandle handle = CurrentHandle;
            SetStatus(ExternalProcessStatus.Stopping);

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
            _ = ObserveExit(handle, launchVersion);
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or ObjectDisposedException)
        {
            CurrentHandle = null;
            SetFaultedSnapshot(exception.Message);
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

            if (ShouldRestart(exit.ExitCode))
            {
                delay = BackoffState.GetNextDelay(handle.StartedAt, exit.ExitedAt);
                Version++;
                restartVersion = Version;
                ApplyRestartPendingResult(exit);
            }
            else
            {
                BackoffState.ResetIfStableRuntimeObserved(handle.StartedAt, exit.ExitedAt);
                ApplyExitedResult(exit, ExternalProcessStatus.Stopped);
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

            LaunchForCurrentVersion();
        }
        finally
        {
            OperationLock.Release();
        }
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
            Snapshot = update(Snapshot);
        }
    }

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
}
