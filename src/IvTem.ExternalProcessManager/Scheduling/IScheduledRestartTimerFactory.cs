namespace IvTem.ExternalProcessManager.Scheduling;

internal interface IScheduledRestartTimerFactory
{
    IScheduledRestartTimer Create(Func<Task> callback);
}
