using IvTem.ExternalProcessManager.Configuration;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal sealed class RestartBackoffState
{
    public RestartBackoffState(EffectiveRestartConfiguration restart)
    {
        ArgumentNullException.ThrowIfNull(restart);

        if (restart.MinBackoff <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(restart), restart.MinBackoff, "MinBackoff must be greater than zero.");

        if (restart.MaxBackoff <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(restart), restart.MaxBackoff, "MaxBackoff must be greater than zero.");

        if (restart.MinBackoff > restart.MaxBackoff)
            throw new ArgumentOutOfRangeException(nameof(restart), restart.MinBackoff, "MinBackoff must be less than or equal to MaxBackoff.");

        Restart = restart;
    }

    private EffectiveRestartConfiguration Restart { get; }

    private TimeSpan? PreviousDelay { get; set; }

    public TimeSpan GetNextDelay(DateTimeOffset startedAt, DateTimeOffset failedAt)
    {
        ResetIfStableRuntimeObserved(startedAt, failedAt);

        TimeSpan delay = PreviousDelay.HasValue
            ? DoubleDelay(PreviousDelay.Value)
            : Restart.MinBackoff;

        PreviousDelay = delay;
        return delay;
    }

    public void ResetIfStableRuntimeObserved(DateTimeOffset startedAt, DateTimeOffset observedAt)
    {
        if (observedAt < startedAt)
            return;

        if (observedAt - startedAt >= Restart.StableRunDuration)
            Reset();
    }

    public void Reset()
        => PreviousDelay = null;

    private TimeSpan DoubleDelay(TimeSpan previousDelay)
    {
        if (previousDelay >= Restart.MaxBackoff)
            return Restart.MaxBackoff;

        if (previousDelay.Ticks >= Restart.MaxBackoff.Ticks / 2)
            return Restart.MaxBackoff;

        TimeSpan doubledDelay = TimeSpan.FromTicks(previousDelay.Ticks * 2);

        if (doubledDelay > Restart.MaxBackoff)
            return Restart.MaxBackoff;

        return doubledDelay;
    }
}
