namespace IvTem.ExternalProcessManager.SampleHost;

internal static class SampleWorkerPath
{
    private const string WorkerProjectName = "IvTem.ExternalProcessManager.SampleWorker";
    private const string WorkerExecutableName = $"{WorkerProjectName}.exe";

    public static string Resolve()
    {
        string publishedWorkerPath = Path.Combine(AppContext.BaseDirectory, WorkerExecutableName);

        if (IsPublishDirectory()
            && File.Exists(publishedWorkerPath))
        {
            return publishedWorkerPath;
        }

        string workerPath = ResolveBuildOutputPath();

        if (File.Exists(workerPath) == false)
            throw new FileNotFoundException("Build or publish the sample worker before running the sample host.", workerPath);

        return workerPath;
    }

    private static string ResolveBuildOutputPath()
    {
        DirectoryInfo samplesDirectory = FindSamplesDirectory();
        DirectoryInfo outputDirectory = new(AppContext.BaseDirectory);
        string configurationName = FindConfigurationName(outputDirectory);
        string workerOutputDirectory = Path.Combine(
            samplesDirectory.FullName,
            WorkerProjectName,
            "bin",
            configurationName,
            "net10.0");
        string runtimeOutputDirectory = Path.Combine(workerOutputDirectory, "win-x64");

        return File.Exists(Path.Combine(runtimeOutputDirectory, WorkerExecutableName))
            ? Path.Combine(runtimeOutputDirectory, WorkerExecutableName)
            : Path.Combine(workerOutputDirectory, WorkerExecutableName);
    }

    private static DirectoryInfo FindSamplesDirectory()
    {
        DirectoryInfo? currentDirectory = new(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            string workerProjectDirectory = Path.Combine(currentDirectory.FullName, WorkerProjectName);

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
