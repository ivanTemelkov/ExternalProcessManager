namespace IvTem.ExternalProcessManager.Lifecycle;

internal static class WindowsPlatform
{
    public static void ThrowIfUnsupported()
    {
        if (OperatingSystem.IsWindows())
            return;

        throw new PlatformNotSupportedException("IvTem.ExternalProcessManager v1 supports Windows only.");
    }
}
