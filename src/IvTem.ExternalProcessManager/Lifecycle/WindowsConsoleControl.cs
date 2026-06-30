using System.Runtime.InteropServices;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal static class WindowsConsoleControl
{
    private const uint CtrlBreakEvent = 1;

    private static ConsoleCtrlHandler IgnoreHandler { get; } = _ => true;

    public static bool TrySendCtrlBreakToProcessGroup(int processGroupId)
    {
        if (GenerateConsoleCtrlEvent(CtrlBreakEvent, (uint)processGroupId))
            return true;

        IntPtr consoleWindow = GetConsoleWindow();

        if ((consoleWindow == IntPtr.Zero) == false)
            return false;

        if (AttachConsole((uint)processGroupId) == false)
            return false;

        bool handlerRegistered = SetConsoleCtrlHandler(IgnoreHandler, add: true);

        try
        {
            return GenerateConsoleCtrlEvent(CtrlBreakEvent, (uint)processGroupId);
        }
        finally
        {
            if (handlerRegistered)
                SetConsoleCtrlHandler(IgnoreHandler, add: false);

            FreeConsole();
        }
    }

    private delegate bool ConsoleCtrlHandler(uint controlType);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GenerateConsoleCtrlEvent(uint controlEvent, uint processGroupId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler handler, bool add);
}
