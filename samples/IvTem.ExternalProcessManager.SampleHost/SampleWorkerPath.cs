namespace IvTem.ExternalProcessManager.SampleHost;

internal static class SampleWorkerPath
{
    public static string Resolve(string workerProjectName)
    {
        string workerExecutableName = $"{workerProjectName}.exe";
        string publishedWorkerPath = Path.Combine(AppContext.BaseDirectory, workerExecutableName);

        if (IsPublishDirectory()
            && File.Exists(publishedWorkerPath))
        {
            return publishedWorkerPath;
        }

        string workerPath = ResolveBuildOutputPath(workerProjectName, workerExecutableName);

        if (File.Exists(workerPath) == false)
            throw new FileNotFoundException("Build or publish the sample worker before running the sample host.", workerPath);

        return workerPath;
    }

    private static string ResolveBuildOutputPath(
        string workerProjectName,
        string workerExecutableName)
    {
        DirectoryInfo samplesDirectory = FindSamplesDirectory(workerProjectName);
        DirectoryInfo outputDirectory = new(AppContext.BaseDirectory);
        string configurationName = FindConfigurationName(outputDirectory);
        string workerOutputDirectory = Path.Combine(
            samplesDirectory.FullName,
            workerProjectName,
            "bin",
            configurationName,
            "net10.0");
        string runtimeOutputDirectory = Path.Combine(workerOutputDirectory, "win-x64");

        return File.Exists(Path.Combine(runtimeOutputDirectory, workerExecutableName))
            ? Path.Combine(runtimeOutputDirectory, workerExecutableName)
            : Path.Combine(workerOutputDirectory, workerExecutableName);
    }

    private static DirectoryInfo FindSamplesDirectory(string workerProjectName)
    {
        DirectoryInfo? currentDirectory = new(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            string workerProjectDirectory = Path.Combine(currentDirectory.FullName, workerProjectName);

            if (Directory.Exists(workerProjectDirectory))
                return currentDirectory;

            currentDirectory = currentDirectory.Parent;
        }

        throw new DirectoryNotFoundException("The samples directory could not be found from the sample host output path.");
    }

    private static string FindConfigurationName(DirectoryInfo outputDirectory)
    {
        DirectoryInfo? currentDirectory = outputDirectory;

        while (currentDirectory is not null)
        {
            if (currentDirectory.Name.Equals("Debug", StringComparison.OrdinalIgnoreCase)
                || currentDirectory.Name.Equals("Release", StringComparison.OrdinalIgnoreCase))
            {
                return currentDirectory.Name;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return "Debug";
    }

    private static bool IsPublishDirectory()
    {
        DirectoryInfo outputDirectory = new(AppContext.BaseDirectory);
        return outputDirectory.Name.Equals("publish", StringComparison.OrdinalIgnoreCase);
    }
}
