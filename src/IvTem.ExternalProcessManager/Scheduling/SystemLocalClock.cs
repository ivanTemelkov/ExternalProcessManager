namespace IvTem.ExternalProcessManager.Scheduling;

internal sealed class SystemLocalClock : ILocalClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;

    public TimeZoneInfo TimeZone => TimeZoneInfo.Local;
}
