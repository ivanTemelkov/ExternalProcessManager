using System.Collections.Immutable;
using IvTem.ExternalProcessManager.Configuration;

namespace IvTem.ExternalProcessManager.Scheduling;

internal sealed class ScheduledRestartCalculator
{
    private const int MaximumDaysToSearch = 14;

    public ScheduledRestartCalculator(TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        TimeZone = timeZone;
    }

    private TimeZoneInfo TimeZone { get; }

    public DateTimeOffset? GetNextOccurrence(
        DateTimeOffset currentLocalTime,
        ImmutableArray<EffectiveScheduledRestartConfiguration> schedules)
    {
        ImmutableArray<DateTimeOffset> occurrences = GetNextOccurrences(currentLocalTime, schedules);

        if (occurrences.IsEmpty)
            return null;

        return occurrences[0];
    }

    public ImmutableArray<DateTimeOffset> GetNextOccurrences(
        DateTimeOffset currentLocalTime,
        ImmutableArray<EffectiveScheduledRestartConfiguration> schedules)
    {
        if (schedules.IsEmpty)
            return [];

        DateTimeOffset currentTime = TimeZoneInfo.ConvertTime(currentLocalTime, TimeZone);
        SortedSet<DateTimeOffset> occurrences = [];

        foreach (EffectiveScheduledRestartConfiguration schedule in schedules)
        {
            DateTimeOffset? occurrence = GetNextOccurrence(currentTime, schedule);

            if (occurrence.HasValue)
                occurrences.Add(occurrence.Value);
        }

        return [.. occurrences];
    }

    private DateTimeOffset? GetNextOccurrence(
        DateTimeOffset currentTime,
        EffectiveScheduledRestartConfiguration schedule)
    {
        for (int daysAhead = 0; daysAhead <= MaximumDaysToSearch; daysAhead++)
        {
            DateTime localDate = currentTime.Date.AddDays(daysAhead);

            if (schedule.Days.Contains(localDate.DayOfWeek) == false)
                continue;

            DateTime candidateLocalTime = localDate.Add(schedule.HourOfDay.ToTimeSpan());
            DateTimeOffset? occurrence = ResolveValidOccurrence(candidateLocalTime, currentTime);

            if (occurrence.HasValue)
                return occurrence.Value;
        }

        return null;
    }

    private DateTimeOffset? ResolveValidOccurrence(DateTime candidateLocalTime, DateTimeOffset currentTime)
    {
        if (TimeZone.IsInvalidTime(candidateLocalTime))
            return null;

        if (TimeZone.IsAmbiguousTime(candidateLocalTime))
            return ResolveAmbiguousOccurrence(candidateLocalTime, currentTime);

        DateTimeOffset occurrence = new(candidateLocalTime, TimeZone.GetUtcOffset(candidateLocalTime));

        if (occurrence > currentTime)
            return occurrence;

        return null;
    }

    private DateTimeOffset? ResolveAmbiguousOccurrence(DateTime candidateLocalTime, DateTimeOffset currentTime)
    {
        DateTimeOffset? nextOccurrence = null;

        foreach (TimeSpan offset in TimeZone.GetAmbiguousTimeOffsets(candidateLocalTime))
        {
            DateTimeOffset occurrence = new(candidateLocalTime, offset);

            if (occurrence <= currentTime)
                continue;

            if (nextOccurrence.HasValue == false || occurrence < nextOccurrence.Value)
                nextOccurrence = occurrence;
        }

        return nextOccurrence;
    }
}
