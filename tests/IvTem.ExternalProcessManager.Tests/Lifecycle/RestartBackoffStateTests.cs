using IvTem.ExternalProcessManager.Configuration;
using IvTem.ExternalProcessManager.Lifecycle;

namespace IvTem.ExternalProcessManager.Tests.Lifecycle;

public sealed class RestartBackoffStateTests
{
    [Fact]
    public void FirstFailureUsesMinimumDelay()
    {
        RestartBackoffState state = new(CreateRestartConfiguration());

        TimeSpan delay = state.GetNextDelay(
            new DateTimeOffset(2026, 6, 30, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 30, 10, 0, 1, TimeSpan.Zero));

        Assert.Equal(TimeSpan.FromSeconds(2), delay);
    }

    [Fact]
    public void SecondFailureDoublesPreviousDelay()
    {
        RestartBackoffState state = new(CreateRestartConfiguration());
        DateTimeOffset startedAt = new(2026, 6, 30, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset failedAt = new(2026, 6, 30, 10, 0, 1, TimeSpan.Zero);

        state.GetNextDelay(startedAt, failedAt);
        TimeSpan delay = state.GetNextDelay(startedAt, failedAt);

        Assert.Equal(TimeSpan.FromSeconds(4), delay);
    }

    [Fact]
    public void RepeatedFailuresCapAtMaximumDelay()
    {
        RestartBackoffState state = new(CreateRestartConfiguration(
            minBackoff: TimeSpan.FromSeconds(3),
            maxBackoff: TimeSpan.FromSeconds(10)));
        DateTimeOffset startedAt = new(2026, 6, 30, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset failedAt = new(2026, 6, 30, 10, 0, 1, TimeSpan.Zero);

        TimeSpan firstDelay = state.GetNextDelay(startedAt, failedAt);
        TimeSpan secondDelay = state.GetNextDelay(startedAt, failedAt);
        TimeSpan thirdDelay = state.GetNextDelay(startedAt, failedAt);
        TimeSpan fourthDelay = state.GetNextDelay(startedAt, failedAt);

        Assert.Equal(TimeSpan.FromSeconds(3), firstDelay);
        Assert.Equal(TimeSpan.FromSeconds(6), secondDelay);
        Assert.Equal(TimeSpan.FromSeconds(10), thirdDelay);
        Assert.Equal(TimeSpan.FromSeconds(10), fourthDelay);
    }

    [Fact]
    public void StableRuntimeResetsToMinimumDelay()
    {
        RestartBackoffState state = new(CreateRestartConfiguration());
        DateTimeOffset startedAt = new(2026, 6, 30, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset failedAt = new(2026, 6, 30, 10, 0, 1, TimeSpan.Zero);

        state.GetNextDelay(startedAt, failedAt);
        state.GetNextDelay(startedAt, failedAt);

        TimeSpan delay = state.GetNextDelay(
            startedAt,
            startedAt.AddMinutes(5));

        Assert.Equal(TimeSpan.FromSeconds(2), delay);
    }

    [Fact]
    public void StableRuntimeCanBeObservedBeforeFailure()
    {
        RestartBackoffState state = new(CreateRestartConfiguration());
        DateTimeOffset firstStartedAt = new(2026, 6, 30, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset firstFailedAt = new(2026, 6, 30, 10, 0, 1, TimeSpan.Zero);
        DateTimeOffset secondStartedAt = new(2026, 6, 30, 10, 1, 0, TimeSpan.Zero);

        state.GetNextDelay(firstStartedAt, firstFailedAt);
        state.GetNextDelay(firstStartedAt, firstFailedAt);

        state.ResetIfStableRuntimeObserved(
            secondStartedAt,
            secondStartedAt.AddMinutes(5));
        TimeSpan delay = state.GetNextDelay(
            secondStartedAt,
            secondStartedAt.AddMinutes(5).AddSeconds(1));

        Assert.Equal(TimeSpan.FromSeconds(2), delay);
    }

    [Fact]
    public void IndependentInstancesDoNotShareState()
    {
        RestartBackoffState firstState = new(CreateRestartConfiguration());
        RestartBackoffState secondState = new(CreateRestartConfiguration());
        DateTimeOffset startedAt = new(2026, 6, 30, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset failedAt = new(2026, 6, 30, 10, 0, 1, TimeSpan.Zero);

        firstState.GetNextDelay(startedAt, failedAt);
        firstState.GetNextDelay(startedAt, failedAt);
        TimeSpan delay = secondState.GetNextDelay(startedAt, failedAt);

        Assert.Equal(TimeSpan.FromSeconds(2), delay);
    }

    private static EffectiveRestartConfiguration CreateRestartConfiguration(
        TimeSpan? minBackoff = null,
        TimeSpan? maxBackoff = null,
        TimeSpan? stableRunDuration = null)
        => new()
        {
            Mode = ExternalProcessRestartMode.NonZeroExitCode,
            MinBackoff = minBackoff ?? TimeSpan.FromSeconds(2),
            MaxBackoff = maxBackoff ?? TimeSpan.FromMinutes(1),
            StableRunDuration = stableRunDuration ?? TimeSpan.FromMinutes(5),
            GracefulStopTimeout = TimeSpan.FromSeconds(10),
        };
}
