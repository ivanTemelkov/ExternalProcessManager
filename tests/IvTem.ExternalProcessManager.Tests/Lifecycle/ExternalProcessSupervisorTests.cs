using System.Collections.Immutable;
using IvTem.ExternalProcessManager.Configuration;
using IvTem.ExternalProcessManager.Lifecycle;
using IvTem.ExternalProcessManager.Scheduling;
using IvTem.ExternalProcessManager.Tests;
using Microsoft.Extensions.Logging;

namespace IvTem.ExternalProcessManager.Tests.Lifecycle;

public sealed class ExternalProcessSupervisorTests
{
    [Fact]
    public async Task StartTransitionsToRunning()
    {
        FakeProcessLauncher launcher = new();
        FakeProcessCleanup cleanup = new();
        ImmediateRestartDelay restartDelay = new();
        FakeLocalClock clock = new();
        FakeScheduledRestartTimerFactory timerFactory = new();
        TestLogger<ExternalProcessSupervisor> logger = new();
        using ExternalProcessSupervisor supervisor = new(CreateConfiguration(), launcher, cleanup, restartDelay, clock, timerFactory, logger);

        await supervisor.Start(CancellationToken.None);

        ExternalProcessSnapshot snapshot = supervisor.GetSnapshot();
        Assert.Equal(ExternalProcessStatus.Running, snapshot.Status);
        Assert.Equal(launcher.LastHandle?.ProcessId, snapshot.ProcessId);
        Assert.Single(launcher.LaunchedHandles);
    }

    [Fact]
    public async Task LaunchFailureTransitionsToFaultedWithoutRestartDelay()
    {
        const string launchError = "The configured process could not be started.";

        FakeProcessLauncher launcher = new()
        {
            LaunchFailure = new InvalidOperationException(launchError),
        };
        FakeProcessCleanup cleanup = new();
        ImmediateRestartDelay restartDelay = new();
        FakeLocalClock clock = new();
        FakeScheduledRestartTimerFactory timerFactory = new();
        TestLogger<ExternalProcessSupervisor> logger = new();
        using ExternalProcessSupervisor supervisor = new(CreateConfiguration(), launcher, cleanup, restartDelay, clock, timerFactory, logger);

        await supervisor.Start(CancellationToken.None);

        ExternalProcessSnapshot snapshot = supervisor.GetSnapshot();
        Assert.Equal(ExternalProcessStatus.Faulted, snapshot.Status);
        Assert.Null(snapshot.ProcessId);
        Assert.Equal(launchError, snapshot.LastError);
        Assert.Equal(0, snapshot.RestartCount);
        Assert.Empty(restartDelay.RequestedDelays);
        Assert.Empty(launcher.LaunchedHandles);
    }

    [Fact]
    public async Task ConcurrentStartDoesNotLaunchDuplicateProcess()
    {
        FakeProcessLauncher launcher = new();
        FakeProcessCleanup cleanup = new();
        ImmediateRestartDelay restartDelay = new();
        FakeLocalClock clock = new();
        FakeScheduledRestartTimerFactory timerFactory = new();
        TestLogger<ExternalProcessSupervisor> logger = new();
        using ExternalProcessSupervisor supervisor = new(CreateConfiguration(), launcher, cleanup, restartDelay, clock, timerFactory, logger);

        await Task.WhenAll(
            supervisor.Start(CancellationToken.None),
            supervisor.Start(CancellationToken.None),
            supervisor.Start(CancellationToken.None));

        Assert.Single(launcher.LaunchedHandles);
        Assert.Equal(ExternalProcessStatus.Running, supervisor.GetSnapshot().Status);
    }

    [Fact]
    public async Task StopTransitionsToStopped()
    {
        FakeProcessLauncher launcher = new();
        FakeProcessCleanup cleanup = new();
        ImmediateRestartDelay restartDelay = new();
        FakeLocalClock clock = new();
        FakeScheduledRestartTimerFactory timerFactory = new();
        using ExternalProcessSupervisor supervisor = new(CreateConfiguration(), launcher, cleanup, restartDelay, clock, timerFactory, new TestLogger<ExternalProcessSupervisor>());

        await supervisor.Start(CancellationToken.None);
        await supervisor.Stop(CancellationToken.None);

        ExternalProcessSnapshot snapshot = supervisor.GetSnapshot();
        Assert.Equal(ExternalProcessStatus.Stopped, snapshot.Status);
        Assert.Null(snapshot.ProcessId);
        Assert.Equal(0, snapshot.LastExitCode);
        Assert.Single(cleanup.StopRequests);
    }

    [Fact]
    public async Task ExitCodeZeroWithDefaultModeStaysStopped()
    {
        FakeProcessLauncher launcher = new();
        FakeProcessCleanup cleanup = new();
        ImmediateRestartDelay restartDelay = new();
        FakeLocalClock clock = new();
        FakeScheduledRestartTimerFactory timerFactory = new();
        using ExternalProcessSupervisor supervisor = new(CreateConfiguration(), launcher, cleanup, restartDelay, clock, timerFactory, new TestLogger<ExternalProcessSupervisor>());

        await supervisor.Start(CancellationToken.None);
        launcher.LastHandle?.CompleteExit(0);

        await WaitUntil(() => supervisor.GetSnapshot().Status == ExternalProcessStatus.Stopped);

        ExternalProcessSnapshot snapshot = supervisor.GetSnapshot();
        Assert.Equal(0, snapshot.LastExitCode);
        Assert.Single(launcher.LaunchedHandles);
    }

    [Fact]
    public async Task NonZeroExitWithDefaultModeRestarts()
    {
        FakeProcessLauncher launcher = new();
        FakeProcessCleanup cleanup = new();
        ImmediateRestartDelay restartDelay = new();
        FakeLocalClock clock = new();
        FakeScheduledRestartTimerFactory timerFactory = new();
        TestLogger<ExternalProcessSupervisor> logger = new();
        using ExternalProcessSupervisor supervisor = new(CreateConfiguration(), launcher, cleanup, restartDelay, clock, timerFactory, logger);

        await supervisor.Start(CancellationToken.None);
        launcher.LastHandle?.CompleteExit(7);

        await WaitUntil(() => launcher.LaunchedHandles.Count == 2);

        ExternalProcessSnapshot snapshot = supervisor.GetSnapshot();
        Assert.Equal(ExternalProcessStatus.Running, snapshot.Status);
        Assert.Equal(1, snapshot.RestartCount);
        Assert.Equal(TimeSpan.FromSeconds(2), Assert.Single(restartDelay.RequestedDelays));
        Assert.Contains(logger.Entries, entry => entry.EventId == 2007
            && entry.Level == LogLevel.Information
            && entry.Message.Contains("00:00:02", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AlwaysRestartsExitCodeZero()
    {
        FakeProcessLauncher launcher = new();
        FakeProcessCleanup cleanup = new();
        ImmediateRestartDelay restartDelay = new();
        FakeLocalClock clock = new();
        using ExternalProcessSupervisor supervisor = new(
            CreateConfiguration(mode: ExternalProcessRestartMode.Always),
            launcher,
            cleanup,
            restartDelay,
            clock,
            new FakeScheduledRestartTimerFactory(),
            new TestLogger<ExternalProcessSupervisor>());

        await supervisor.Start(CancellationToken.None);
        launcher.LastHandle?.CompleteExit(0);

        await WaitUntil(() => launcher.LaunchedHandles.Count == 2);

        Assert.Equal(ExternalProcessStatus.Running, supervisor.GetSnapshot().Status);
        Assert.Equal(1, supervisor.GetSnapshot().RestartCount);
    }

    [Fact]
    public async Task NeverDoesNotRestartNonZeroExit()
    {
        FakeProcessLauncher launcher = new();
        FakeProcessCleanup cleanup = new();
        ImmediateRestartDelay restartDelay = new();
        FakeLocalClock clock = new();
        using ExternalProcessSupervisor supervisor = new(
            CreateConfiguration(mode: ExternalProcessRestartMode.Never),
            launcher,
            cleanup,
            restartDelay,
            clock,
            new FakeScheduledRestartTimerFactory(),
            new TestLogger<ExternalProcessSupervisor>());

        await supervisor.Start(CancellationToken.None);
        launcher.LastHandle?.CompleteExit(7);

        await WaitUntil(() => supervisor.GetSnapshot().Status == ExternalProcessStatus.Stopped);

        Assert.Single(launcher.LaunchedHandles);
        Assert.Equal(7, supervisor.GetSnapshot().LastExitCode);
    }

    [Fact]
    public async Task IntentionalStopDoesNotRestart()
    {
        FakeProcessLauncher launcher = new();
        FakeProcessCleanup cleanup = new();
        ImmediateRestartDelay restartDelay = new();
        FakeLocalClock clock = new();
        FakeScheduledRestartTimerFactory timerFactory = new();
        using ExternalProcessSupervisor supervisor = new(CreateConfiguration(), launcher, cleanup, restartDelay, clock, timerFactory, new TestLogger<ExternalProcessSupervisor>());

        await supervisor.Start(CancellationToken.None);
        await supervisor.Stop(CancellationToken.None);
        launcher.LastHandle?.CompleteExit(7);

        await Task.Delay(TimeSpan.FromMilliseconds(25));

        Assert.Single(launcher.LaunchedHandles);
        Assert.Equal(ExternalProcessStatus.Stopped, supervisor.GetSnapshot().Status);
    }

    [Fact]
    public async Task BackoffDelayIsRequestedBeforeRestart()
    {
        FakeProcessLauncher launcher = new();
        FakeProcessCleanup cleanup = new();
        BlockingRestartDelay restartDelay = new();
        FakeLocalClock clock = new();
        FakeScheduledRestartTimerFactory timerFactory = new();
        using ExternalProcessSupervisor supervisor = new(CreateConfiguration(), launcher, cleanup, restartDelay, clock, timerFactory, new TestLogger<ExternalProcessSupervisor>());

        await supervisor.Start(CancellationToken.None);
        launcher.LastHandle?.CompleteExit(7);

        await WaitUntil(() => restartDelay.RequestedDelays.Length == 1);

        Assert.Single(launcher.LaunchedHandles);
        Assert.Equal(ExternalProcessStatus.RestartPending, supervisor.GetSnapshot().Status);

        restartDelay.Complete();
        await WaitUntil(() => launcher.LaunchedHandles.Count == 2);

        Assert.Equal(ExternalProcessStatus.Running, supervisor.GetSnapshot().Status);
    }

    [Fact]
    public void GetSnapshotIncludesNextScheduledRestart()
    {
        FakeProcessLauncher launcher = new();
        FakeProcessCleanup cleanup = new();
        ImmediateRestartDelay restartDelay = new();
        FakeLocalClock clock = new()
        {
            Now = new DateTimeOffset(2026, 6, 30, 10, 0, 0, TimeSpan.Zero),
            TimeZone = TimeZoneInfo.Utc,
        };
        using ExternalProcessSupervisor supervisor = new(
            CreateConfiguration(scheduledRestarts:
            [
                new EffectiveScheduledRestartConfiguration
                {
                    Path = "ExternalProcessManager:Processes:0:ScheduledRestarts:0",
                    HourOfDay = new TimeOnly(12, 30),
                    Days = [DayOfWeek.Tuesday],
                },
            ]),
            launcher,
            cleanup,
            restartDelay,
            clock,
            new FakeScheduledRestartTimerFactory(),
            new TestLogger<ExternalProcessSupervisor>());

        ExternalProcessSnapshot snapshot = supervisor.GetSnapshot();

        Assert.Equal(new DateTimeOffset(2026, 6, 30, 12, 30, 0, TimeSpan.Zero), snapshot.NextScheduledRestart);
    }

    [Fact]
    public async Task DueScheduledRestartRestartsRunningProcess()
    {
        FakeProcessLauncher launcher = new();
        FakeProcessCleanup cleanup = new();
        ImmediateRestartDelay restartDelay = new();
        FakeLocalClock clock = new()
        {
            Now = new DateTimeOffset(2026, 6, 30, 10, 0, 0, TimeSpan.Zero),
            TimeZone = TimeZoneInfo.Utc,
        };
        FakeScheduledRestartTimerFactory timerFactory = new();
        using ExternalProcessSupervisor supervisor = new(
            CreateConfiguration(scheduledRestarts: CreateTuesdayRestartSchedules(new TimeOnly(12, 30))),
            launcher,
            cleanup,
            restartDelay,
            clock,
            timerFactory,
            new TestLogger<ExternalProcessSupervisor>());

        await supervisor.Start(CancellationToken.None);

        FakeScheduledRestartTimer timer = Assert.Single(timerFactory.Timers);
        Assert.Equal(new DateTimeOffset(2026, 6, 30, 12, 30, 0, TimeSpan.Zero), timer.ScheduledDueTime);

        DateTimeOffset dueTime = Assert.IsType<DateTimeOffset>(timer.ScheduledDueTime);
        clock.Now = dueTime;
        await timer.Trigger();

        ExternalProcessSnapshot snapshot = supervisor.GetSnapshot();
        Assert.Equal(ExternalProcessStatus.Running, snapshot.Status);
        Assert.Equal(1, snapshot.RestartCount);
        Assert.Equal(2, launcher.LaunchedHandles.Count);
        Assert.Single(cleanup.StopRequests);
    }

    [Fact]
    public async Task ScheduledRestartUpdatesNextOccurrenceAfterExecution()
    {
        FakeProcessLauncher launcher = new();
        FakeProcessCleanup cleanup = new();
        ImmediateRestartDelay restartDelay = new();
        FakeLocalClock clock = new()
        {
            Now = new DateTimeOffset(2026, 6, 30, 10, 0, 0, TimeSpan.Zero),
            TimeZone = TimeZoneInfo.Utc,
        };
        FakeScheduledRestartTimerFactory timerFactory = new();
        using ExternalProcessSupervisor supervisor = new(
            CreateConfiguration(scheduledRestarts: CreateTuesdayRestartSchedules(new TimeOnly(12, 30))),
            launcher,
            cleanup,
            restartDelay,
            clock,
            timerFactory,
            new TestLogger<ExternalProcessSupervisor>());

        await supervisor.Start(CancellationToken.None);
        FakeScheduledRestartTimer timer = Assert.Single(timerFactory.Timers);

        DateTimeOffset dueTime = Assert.IsType<DateTimeOffset>(timer.ScheduledDueTime);
        clock.Now = dueTime;
        await timer.Trigger();

        Assert.Equal(new DateTimeOffset(2026, 7, 7, 12, 30, 0, TimeSpan.Zero), supervisor.GetSnapshot().NextScheduledRestart);
        Assert.Equal(new DateTimeOffset(2026, 7, 7, 12, 30, 0, TimeSpan.Zero), timer.ScheduledDueTime);
    }

    [Fact]
    public async Task DuplicateDueSchedulesCauseOneRestart()
    {
        FakeProcessLauncher launcher = new();
        FakeProcessCleanup cleanup = new();
        ImmediateRestartDelay restartDelay = new();
        FakeLocalClock clock = new()
        {
            Now = new DateTimeOffset(2026, 6, 30, 10, 0, 0, TimeSpan.Zero),
            TimeZone = TimeZoneInfo.Utc,
        };
        FakeScheduledRestartTimerFactory timerFactory = new();
        using ExternalProcessSupervisor supervisor = new(
            CreateConfiguration(scheduledRestarts:
            [
                .. CreateTuesdayRestartSchedules(new TimeOnly(12, 30)),
                .. CreateTuesdayRestartSchedules(new TimeOnly(12, 30)),
            ]),
            launcher,
            cleanup,
            restartDelay,
            clock,
            timerFactory,
            new TestLogger<ExternalProcessSupervisor>());

        await supervisor.Start(CancellationToken.None);
        FakeScheduledRestartTimer timer = Assert.Single(timerFactory.Timers);

        DateTimeOffset dueTime = Assert.IsType<DateTimeOffset>(timer.ScheduledDueTime);
        clock.Now = dueTime;
        await timer.Trigger();

        Assert.Equal(2, launcher.LaunchedHandles.Count);
        Assert.Single(cleanup.StopRequests);
        Assert.Equal(1, supervisor.GetSnapshot().RestartCount);
    }

    [Fact]
    public async Task DueScheduledRestartStartsStoppedSupervisedProcess()
    {
        FakeProcessLauncher launcher = new();
        FakeProcessCleanup cleanup = new();
        ImmediateRestartDelay restartDelay = new();
        FakeLocalClock clock = new()
        {
            Now = new DateTimeOffset(2026, 6, 30, 10, 0, 0, TimeSpan.Zero),
            TimeZone = TimeZoneInfo.Utc,
        };
        FakeScheduledRestartTimerFactory timerFactory = new();
        using ExternalProcessSupervisor supervisor = new(
            CreateConfiguration(scheduledRestarts: CreateTuesdayRestartSchedules(new TimeOnly(12, 30))),
            launcher,
            cleanup,
            restartDelay,
            clock,
            timerFactory,
            new TestLogger<ExternalProcessSupervisor>());

        await supervisor.Start(CancellationToken.None);
        launcher.LastHandle?.CompleteExit(0);
        await WaitUntil(() => supervisor.GetSnapshot().Status == ExternalProcessStatus.Stopped);

        FakeScheduledRestartTimer timer = Assert.Single(timerFactory.Timers);
        DateTimeOffset dueTime = Assert.IsType<DateTimeOffset>(timer.ScheduledDueTime);
        clock.Now = dueTime;
        await timer.Trigger();

        ExternalProcessSnapshot snapshot = supervisor.GetSnapshot();
        Assert.Equal(ExternalProcessStatus.Running, snapshot.Status);
        Assert.Equal(2, launcher.LaunchedHandles.Count);
        Assert.Empty(cleanup.StopRequests);
        Assert.Equal(1, snapshot.RestartCount);
    }

    private static EffectiveExternalProcessConfiguration CreateConfiguration(
        ExternalProcessRestartMode mode = ExternalProcessRestartMode.NonZeroExitCode,
        ImmutableArray<EffectiveScheduledRestartConfiguration> scheduledRestarts = default)
        => new()
        {
            Path = "ExternalProcessManager:Processes:0",
            Alias = "worker",
            AliasKey = "worker",
            FileName = "worker.exe",
            ArgumentMode = EffectiveProcessArgumentMode.ArgumentList,
            ArgumentList = ["--run"],
            Restart = new EffectiveRestartConfiguration
            {
                Mode = mode,
                MinBackoff = TimeSpan.FromSeconds(2),
                MaxBackoff = TimeSpan.FromMinutes(1),
                StableRunDuration = TimeSpan.FromMinutes(5),
                GracefulStopTimeout = TimeSpan.FromSeconds(10),
            },
            ScheduledRestarts = scheduledRestarts.IsDefault ? [] : scheduledRestarts,
        };

    private static ImmutableArray<EffectiveScheduledRestartConfiguration> CreateTuesdayRestartSchedules(TimeOnly hourOfDay)
        =>
        [
            new()
            {
                Path = "ExternalProcessManager:Processes:0:ScheduledRestarts:0",
                HourOfDay = hourOfDay,
                Days = [DayOfWeek.Tuesday],
            },
        ];

    private static async Task WaitUntil(Func<bool> condition)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(5);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (condition())
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }

        Assert.True(condition(), "Condition was not satisfied before the timeout.");
    }

    private sealed class FakeProcessLauncher : IProcessLauncher
    {
        private int NextProcessId { get; set; } = 1000;

        public Exception? LaunchFailure { get; init; }

        public List<FakeProcessHandle> LaunchedHandles { get; } = [];

        public FakeProcessHandle? LastHandle => LaunchedHandles.Count == 0
            ? null
            : LaunchedHandles[^1];

        public IProcessHandle Launch(EffectiveExternalProcessConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            if (LaunchFailure is not null)
                throw LaunchFailure;

            FakeProcessHandle handle = new(NextProcessId++, DateTimeOffset.UtcNow);
            LaunchedHandles.Add(handle);
            return handle;
        }
    }

    private sealed class FakeProcessCleanup : IProcessCleanup
    {
        public List<TimeSpan> StopRequests { get; } = [];

        public async Task<ProcessCleanupResult> Stop(
            IProcessHandle handle,
            TimeSpan gracefulStopTimeout,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(handle);

            StopRequests.Add(gracefulStopTimeout);

            if (handle is FakeProcessHandle fakeHandle && handle.Exited.IsCompleted == false)
                fakeHandle.CompleteExit(0);

            ProcessExitResult exit = await handle.Exited.WaitAsync(cancellationToken);

            return new ProcessCleanupResult
            {
                ProcessId = handle.ProcessId,
                Outcome = ProcessCleanupOutcome.GracefulStop,
                ExitCode = exit.ExitCode,
                CompletedAt = exit.ExitedAt,
            };
        }
    }

    private sealed class FakeProcessHandle : IProcessHandle
    {
        private TaskCompletionSource<ProcessExitResult> ExitCompletion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public FakeProcessHandle(int processId, DateTimeOffset startedAt)
        {
            ProcessId = processId;
            StartedAt = startedAt;
        }

        public int ProcessId { get; }

        public DateTimeOffset StartedAt { get; }

        public Task<ProcessExitResult> Exited => ExitCompletion.Task;

        public void CompleteExit(int? exitCode)
            => ExitCompletion.TrySetResult(new ProcessExitResult
            {
                ExitCode = exitCode,
                ExitedAt = DateTimeOffset.UtcNow,
            });

        public void Dispose()
        {
            if (ExitCompletion.Task.IsCompleted == false)
                ExitCompletion.TrySetCanceled();
        }
    }

    private sealed class ImmediateRestartDelay : IRestartDelay
    {
        private ImmutableArray<TimeSpan>.Builder RequestedDelayBuilder { get; } =
            ImmutableArray.CreateBuilder<TimeSpan>();

        public ImmutableArray<TimeSpan> RequestedDelays => RequestedDelayBuilder.ToImmutable();

        public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
            RequestedDelayBuilder.Add(delay);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class BlockingRestartDelay : IRestartDelay
    {
        private TaskCompletionSource Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private ImmutableArray<TimeSpan>.Builder RequestedDelayBuilder { get; } =
            ImmutableArray.CreateBuilder<TimeSpan>();

        public ImmutableArray<TimeSpan> RequestedDelays => RequestedDelayBuilder.ToImmutable();

        public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
            RequestedDelayBuilder.Add(delay);
            return Completion.Task.WaitAsync(cancellationToken);
        }

        public void Complete()
            => Completion.TrySetResult();
    }

    private sealed class FakeLocalClock : ILocalClock
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

        public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Utc;
    }

    private sealed class FakeScheduledRestartTimerFactory : IScheduledRestartTimerFactory
    {
        public List<FakeScheduledRestartTimer> Timers { get; } = [];

        public IScheduledRestartTimer Create(Func<Task> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            FakeScheduledRestartTimer timer = new(callback);
            Timers.Add(timer);
            return timer;
        }
    }

    private sealed class FakeScheduledRestartTimer : IScheduledRestartTimer
    {
        public FakeScheduledRestartTimer(Func<Task> callback)
        {
            Callback = callback;
        }

        private Func<Task> Callback { get; }

        public DateTimeOffset? ScheduledDueTime { get; private set; }

        public bool IsDisposed { get; private set; }

        public void Schedule(DateTimeOffset dueTime)
            => ScheduledDueTime = dueTime;

        public void Cancel()
            => ScheduledDueTime = null;

        public Task Trigger()
            => Callback();

        public void Dispose()
            => IsDisposed = true;
    }
}
