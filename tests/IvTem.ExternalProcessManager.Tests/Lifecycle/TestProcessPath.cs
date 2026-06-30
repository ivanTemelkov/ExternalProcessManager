namespace IvTem.ExternalProcessManager.Tests.Lifecycle;

internal static class TestProcessPath
{
    public static string Resolve()
    {
        string? directory = AppContext.BaseDirectory;

        while (directory is not null)
        {
            string helperBinDirectory = Path.Combine(
                directory,
                "tests",
                "IvTem.ExternalProcessManager.TestProcess",
                "bin");

            if (Directory.Exists(helperBinDirectory))
            {
                string? candidate = Directory
                    .EnumerateFiles(
                        helperBinDirectory,
                        "IvTem.ExternalProcessManager.TestProcess.exe",
                        SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();

                if (candidate is not null)
                    return candidate;
            }

            DirectoryInfo? parent = Directory.GetParent(directory);
            directory = parent?.FullName;
        }

        throw new FileNotFoundException("The test helper executable was not found.");
    }
}
