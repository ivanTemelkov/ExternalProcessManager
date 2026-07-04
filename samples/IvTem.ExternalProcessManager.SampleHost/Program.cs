using IvTem.ExternalProcessManager;
using IvTem.ExternalProcessManager.SampleHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

if (OperatingSystem.IsWindows() == false)
{
    await Console.Error.WriteLineAsync("IvTem.ExternalProcessManager samples require Windows process-control APIs.")
        .ConfigureAwait(continueOnCapturedContext: false);
    return 1;
}

HostApplicationBuilderSettings settings = new()
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
};

HostApplicationBuilder builder = Host.CreateApplicationBuilder(settings);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

string workerPath = SampleWorkerPath.Resolve("IvTem.ExternalProcessManager.SampleWorker");
string failingWorkerPath = SampleWorkerPath.Resolve("IvTem.ExternalProcessManager.FailingSampleWorker");
string workerWorkingDirectory = Path.GetDirectoryName(workerPath) is string directory
    ? directory
    : AppContext.BaseDirectory;
string failingWorkerWorkingDirectory = Path.GetDirectoryName(failingWorkerPath) is string failingWorkerDirectory
    ? failingWorkerDirectory
    : AppContext.BaseDirectory;

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
{
    ["ExternalProcessManager:Processes:0:FileName"] = workerPath,
    ["ExternalProcessManager:Processes:0:WorkingDirectory"] = workerWorkingDirectory,
    ["ExternalProcessManager:Processes:0:ArgumentList:0"] = "--name",
    ["ExternalProcessManager:Processes:0:ArgumentList:1"] = "sample-worker",
    ["ExternalProcessManager:Processes:0:ArgumentList:2"] = "--heartbeat-seconds",
    ["ExternalProcessManager:Processes:0:ArgumentList:3"] = "1",
    ["ExternalProcessManager:Processes:1:FileName"] = failingWorkerPath,
    ["ExternalProcessManager:Processes:1:WorkingDirectory"] = failingWorkerWorkingDirectory,
});

builder.Services.AddExternalProcessManager(
    builder.Configuration.GetSection("ExternalProcessManager"));
builder.Services.AddHostedService<SampleDiagnosticsService>();

using IHost host = builder.Build();
await host.RunAsync().ConfigureAwait(continueOnCapturedContext: false);

return 0;
