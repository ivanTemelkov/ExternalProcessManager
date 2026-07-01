using IvTem.ExternalProcessManager.Scheduling;
using Microsoft.Extensions.Logging;

namespace IvTem.ExternalProcessManager.Tests.Scheduling;

public sealed class SystemScheduledRestartTimerTests
{
    [Fact]
    public async Task CallbackFailureIsLogged()
    {
        FakeLocalClock clock = new();
        TestLogger<SystemScheduledRestartTimer> logger = new();
        InvalidOperationException expectedException = new("Callback failed.");
        using SystemScheduledRestartTimer timer = new(
            () => Task.FromException(expectedException),
            clock,
            logger);

        timer.Schedule(clock.Now);

        await WaitUntil(() => logger.Entries.Count == 1);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal(4000, entry.EventId);
        Assert.Same(expectedException, entry.Exception);
    }

    [Fact]
    public async Task CallbackCancellationIsBenign()
    {
        FakeLocalClock clock = new();
        TestLogger<SystemScheduledRestartTimer> logger = new();
        TaskCompletionSource callbackInvoked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using SystemScheduledRestartTimer timer = new(
            () =>
            {
                callbackInvoked.SetResult();
                return Task.FromCanceled(new CancellationToken(canceled: true));
            },
            clock,
            logger);

        timer.Schedule(clock.Now);

        await callbackInvoked.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(TimeSpan.FromMilliseconds(25));

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task CallbackDisposalRaceIsBenign()
    {
        FakeLocalClock clock = new();
        TestLogger<SystemScheduledRestartTimer> logger = new();
        TaskCompletionSource callbackInvoked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using SystemScheduledRestartTimer timer = new(
            () =>
            {
                callbackInvoked.SetResult();
                return Task.FromException(new ObjectDisposedException("timer"));
            },
            clock,
            logger);

        timer.Schedule(clock.Now);

        await callbackInvoked.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(TimeSpan.FromMilliseconds(25));

        Assert.Empty(logger.Entries);
    }

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

    private sealed class FakeLocalClock : ILocalClock
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

        public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Utc;
    }
}
