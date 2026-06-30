using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace IvTem.ExternalProcessManager.Lifecycle;

internal static class WindowsProcessCreation
{
    private const uint CreateNewProcessGroup = 0x00000200;
    private const uint CreateUnicodeEnvironment = 0x00000400;

    public static Process Start(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        string commandLineText = WindowsCommandLineBuilder.Build(
            startInfo.FileName,
            startInfo.Arguments,
            startInfo.ArgumentList);
        StringBuilder commandLine = new(commandLineText);
        string environment = WindowsEnvironmentBlockBuilder.Build(startInfo.EnvironmentVariables);
        string? workingDirectory = string.IsNullOrWhiteSpace(startInfo.WorkingDirectory)
            ? null
            : startInfo.WorkingDirectory;

        StartupInfo startupInfo = new()
        {
            Size = Marshal.SizeOf<StartupInfo>(),
        };

        bool started = CreateProcess(
            applicationName: null,
            commandLine,
            processAttributes: IntPtr.Zero,
            threadAttributes: IntPtr.Zero,
            inheritHandles: false,
            creationFlags: CreateNewProcessGroup | CreateUnicodeEnvironment,
            environment,
            currentDirectory: workingDirectory,
            startupInfo,
            out ProcessInformation processInformation);

        if (started == false)
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        try
        {
            return Process.GetProcessById(processInformation.ProcessId);
        }
        finally
        {
            CloseHandle(processInformation.ThreadHandle);
            CloseHandle(processInformation.ProcessHandle);
        }
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateProcessW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcess(
        string? applicationName,
        StringBuilder commandLine,
        IntPtr processAttributes,
        IntPtr threadAttributes,
        bool inheritHandles,
        uint creationFlags,
        string environment,
        string? currentDirectory,
        in StartupInfo startupInfo,
        out ProcessInformation processInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private readonly struct StartupInfo
    {
        public int Size { get; init; }

        private readonly string? Reserved;

        private readonly string? Desktop;

        private readonly string? Title;

        private readonly int PositionX;

        private readonly int PositionY;

        private readonly int SizeX;

        private readonly int SizeY;

        private readonly int CountCharsX;

        private readonly int CountCharsY;

        private readonly int FillAttribute;

        private readonly int Flags;

        private readonly short ShowWindow;

        private readonly short Reserved2;

        private readonly IntPtr Reserved2Pointer;

        private readonly IntPtr StandardInput;

        private readonly IntPtr StandardOutput;

        private readonly IntPtr StandardError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct ProcessInformation
    {
        public IntPtr ProcessHandle { get; init; }

        public IntPtr ThreadHandle { get; init; }

        public int ProcessId { get; init; }

        private readonly int ThreadId;
    }
}
