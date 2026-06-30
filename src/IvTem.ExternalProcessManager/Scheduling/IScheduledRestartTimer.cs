namespace IvTem.ExternalProcessManager.Scheduling;

internal interface IScheduledRestartTimer : IDisposable
{
    void Schedule(DateTimeOffset dueTime);

    void Cancel();
}
