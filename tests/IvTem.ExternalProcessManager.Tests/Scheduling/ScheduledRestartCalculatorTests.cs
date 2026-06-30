using System.Collections.Immutable;
using IvTem.ExternalProcessManager.Configuration;
using IvTem.ExternalProcessManager.Scheduling;

namespace IvTem.ExternalProcessManager.Tests.Scheduling;

public sealed class ScheduledRestartCalculatorTests
{
    private static TimeZoneInfo FixedTimeZone { get; } = TimeZoneInfo.CreateCustomTimeZone(
        "Fixed Test Time",
        TimeSpan.FromHours(2),
        "Fixed Test Time",
        "Fixed Test Time");

    private static TimeZoneInfo DaylightSavingTimeZone { get; } = CreateDaylightSavingTimeZone();

    [Fact]
    public void ScheduleLaterTodayReturnsToday()
    {
        ScheduledRestartCalculator calculator = new(FixedTimeZone);
        EffectiveScheduledRestartConfiguration schedule = CreateSchedule(
            new TimeOnly(11, 30),
            DayOfWeek.Wednesday);

        DateTimeOffset? occurrence = calculator.GetNextOccurrence(
            new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.FromHours(2)),
            [schedule]);

        Assert.Equal(
            new DateTimeOffset(2026, 6, 24, 11, 30, 0, TimeSpan.FromHours(2)),
            occurrence);
    }

    [Fact]
    public void ScheduleEarlierTodayRollsForwardToNextAllowedDay()
    {
        ScheduledRestartCalculator calculator = new(FixedTimeZone);
        EffectiveScheduledRestartConfiguration schedule = CreateSchedule(
            new TimeOnly(9, 0),
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday);

        DateTimeOffset? occurrence = calculator.GetNextOccurrence(
            new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.FromHours(2)),
            [schedule]);

        Assert.Equal(
            new DateTimeOffset(2026, 6, 25, 9, 0, 0, TimeSpan.FromHours(2)),
            occurrence);
    }

    [Fact]
    public void ScheduleForLaterWeekdayReturnsThatWeekday()
    {
        ScheduledRestartCalculator calculator = new(FixedTimeZone);
        EffectiveScheduledRestartConfiguration schedule = CreateSchedule(
            new TimeOnly(4, 0),
            DayOfWeek.Friday);

        DateTimeOffset? occurrence = calculator.GetNextOccurrence(
            new DateTimeOffset(2026, 6, 22, 10, 0, 0, TimeSpan.FromHours(2)),
            [schedule]);

        Assert.Equal(
            new DateTimeOffset(2026, 6, 26, 4, 0, 0, TimeSpan.FromHours(2)),
            occurrence);
    }

    [Fact]
    public void ScheduleRollsToNextWeekWhenNeeded()
    {
        ScheduledRestartCalculator calculator = new(FixedTimeZone);
        EffectiveScheduledRestartConfiguration schedule = CreateSchedule(
            new TimeOnly(9, 0),
            DayOfWeek.Wednesday);

        DateTimeOffset? occurrence = calculator.GetNextOccurrence(
            new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.FromHours(2)),
            [schedule]);

        Assert.Equal(
            new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.FromHours(2)),
            occurrence);
    }

    [Fact]
    public void MultipleSchedulesReturnEarliestOccurrence()
    {
        ScheduledRestartCalculator calculator = new(FixedTimeZone);

        DateTimeOffset? occurrence = calculator.GetNextOccurrence(
            new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.FromHours(2)),
            [
                CreateSchedule(new TimeOnly(22, 0), DayOfWeek.Wednesday),
                CreateSchedule(new TimeOnly(11, 0), DayOfWeek.Wednesday),
            ]);

        Assert.Equal(
            new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.FromHours(2)),
            occurrence);
    }

    [Fact]
    public void DuplicateSchedulesCollapseToOneOccurrence()
    {
        ScheduledRestartCalculator calculator = new(FixedTimeZone);

        ImmutableArray<DateTimeOffset> occurrences = calculator.GetNextOccurrences(
            new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.FromHours(2)),
            [
                CreateSchedule(new TimeOnly(11, 0), DayOfWeek.Wednesday),
                CreateSchedule(new TimeOnly(11, 0), DayOfWeek.Wednesday),
            ]);

        DateTimeOffset occurrence = Assert.Single(occurrences);
        Assert.Equal(
            new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.FromHours(2)),
            occurrence);
    }

    [Fact]
    public void MidnightTransitionUsesNextLocalDate()
    {
        ScheduledRestartCalculator calculator = new(FixedTimeZone);
        EffectiveScheduledRestartConfiguration schedule = CreateSchedule(
            TimeOnly.MinValue,
            DayOfWeek.Wednesday);

        DateTimeOffset? occurrence = calculator.GetNextOccurrence(
            new DateTimeOffset(2026, 6, 23, 23, 59, 0, TimeSpan.FromHours(2)),
            [schedule]);

        Assert.Equal(
            new DateTimeOffset(2026, 6, 24, 0, 0, 0, TimeSpan.FromHours(2)),
            occurrence);
    }

    [Fact]
    public void DaylightSavingGapSkipsInvalidLocalOccurrence()
    {
        ScheduledRestartCalculator calculator = new(DaylightSavingTimeZone);
        EffectiveScheduledRestartConfiguration schedule = CreateSchedule(
            new TimeOnly(2, 30),
            DayOfWeek.Sunday,
            DayOfWeek.Monday);

        DateTimeOffset? occurrence = calculator.GetNextOccurrence(
            new DateTimeOffset(2026, 3, 29, 1, 0, 0, TimeSpan.FromHours(1)),
            [schedule]);

        Assert.Equal(
            new DateTimeOffset(2026, 3, 30, 2, 30, 0, TimeSpan.FromHours(2)),
            occurrence);
    }

    [Fact]
    public void DaylightSavingRepeatedLocalTimeProducesOneConfiguredOccurrence()
    {
        ScheduledRestartCalculator calculator = new(DaylightSavingTimeZone);
        EffectiveScheduledRestartConfiguration schedule = CreateSchedule(
            new TimeOnly(2, 30),
            DayOfWeek.Sunday);

        ImmutableArray<DateTimeOffset> occurrences = calculator.GetNextOccurrences(
            new DateTimeOffset(2026, 10, 25, 1, 0, 0, TimeSpan.FromHours(2)),
            [schedule]);

        DateTimeOffset occurrence = Assert.Single(occurrences);
        Assert.Equal(
            new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(2)),
            occurrence);
    }

    private static EffectiveScheduledRestartConfiguration CreateSchedule(
        TimeOnly hourOfDay,
        params DayOfWeek[] days)
        => new()
        {
            Path = "Test",
            HourOfDay = hourOfDay,
            Days = [.. days],
        };

    private static TimeZoneInfo CreateDaylightSavingTimeZone()
    {
        TimeZoneInfo.TransitionTime startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            month: 3,
            week: 5,
            DayOfWeek.Sunday);
        TimeZoneInfo.TransitionTime endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 3, 0, 0),
            month: 10,
            week: 5,
            DayOfWeek.Sunday);
        TimeZoneInfo.AdjustmentRule adjustmentRule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31),
            TimeSpan.FromHours(1),
            startTransition,
            endTransition);

        return TimeZoneInfo.CreateCustomTimeZone(
            "DST Test Time",
            TimeSpan.FromHours(1),
            "DST Test Time",
            "DST Test Time",
            "DST Test Daylight Time",
            [adjustmentRule]);
    }
}
