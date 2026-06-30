namespace IvTem.ExternalProcessManager.Scheduling;

internal interface ILocalClock
{
    DateTimeOffset Now { get; }

    TimeZoneInfo TimeZone { get; }
}
