using IvTem.ExternalProcessManager;
using IvTem.ExternalProcessManager.Scheduling;
using IvTem.ExternalProcessManager.Tests.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IvTem.ExternalProcessManager.Tests;

public sealed class PublicContractsTests
{
    [Fact]
    public async Task ServiceCollectionExtensionRegistersManagerContract()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .Build();
        ServiceCollection services = new();

        IServiceCollection returnedServices = services.AddExternalProcessManager(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        IExternalProcessManager manager = provider.GetRequiredService<IExternalProcessManager>();
        ILocalClock clock = provider.GetRequiredService<ILocalClock>();
        IHostedService hostedService = Assert.Single(provider.GetServices<IHostedService>());

        await manager.StartAsync();
        ExternalProcessManagerSnapshot snapshot = manager.GetSnapshot();
        await manager.StopAsync();

        Assert.Same(services, returnedServices);
        Assert.IsType<SystemLocalClock>(clock);
        Assert.Equal("ExternalProcessManagerHostedService", hostedService.GetType().Name);
        Assert.True(snapshot.IsRunning);
        Assert.Empty(snapshot.Processes);
        Assert.Empty(snapshot.ValidationErrors);
    }

    [Fact]
    public void ServiceCollectionExtensionDoesNotRegisterDuplicateManagersOrHostedServices()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .Build();
        ServiceCollection services = new();

        services.AddExternalProcessManager(configuration);
        services.AddExternalProcessManager(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Single(provider.GetServices<IExternalProcessManager>());
        Assert.Single(provider.GetServices<IHostedService>());
    }

    [Fact]
    public async Task GenericHostStartsAndStopsManagedProcesses()
    {
        if (OperatingSystem.IsWindows() == false)
            return;

        using TemporaryFile readyFile = TemporaryFile.Create();
        using TemporaryFile stoppedFile = TemporaryFile.Create();
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["ExternalProcessManager:Processes:0:Alias"] = "worker-a",
            ["ExternalProcessManager:Processes:0:FileName"] = TestProcessPath.Resolve(),
            ["ExternalProcessManager:Processes:0:ArgumentList:0"] = "handle-ctrl-break",
            ["ExternalProcessManager:Processes:0:ArgumentList:1"] = "--ready-file",
            ["ExternalProcessManager:Processes:0:ArgumentList:2"] = readyFile.Path,
            ["ExternalProcessManager:Processes:0:ArgumentList:3"] = "--stopped-file",
            ["ExternalProcessManager:Processes:0:ArgumentList:4"] = stoppedFile.Path,
        };

        builder.Configuration.AddInMemoryCollection(values);
        builder.Services.AddExternalProcessManager(builder.Configuration.GetSection("ExternalProcessManager"));

        using IHost host = builder.Build();

        await host.StartAsync();

        await readyFile.WaitUntilExists();

        IExternalProcessManager manager = host.Services.GetRequiredService<IExternalProcessManager>();
        ExternalProcessSnapshot runningProcess = Assert.Single(manager.GetSnapshot().Processes);
        Assert.Equal(ExternalProcessStatus.Running, runningProcess.Status);

        await host.StopAsync();

        await stoppedFile.WaitUntilExists();

        Assert.False(manager.GetSnapshot().IsRunning);
        Assert.Equal(ExternalProcessStatus.Stopped, manager.GetSnapshot().Processes[0].Status);
    }

    [Fact]
    public void SnapshotRecordsCanBeConstructed()
    {
        ExternalProcessValidationError validationError = new()
        {
            Alias = "worker-a",
            Path = "Processes[0].Alias",
            Message = "Alias is required.",
        };
        ExternalProcessSnapshot processSnapshot = new()
        {
            Alias = "worker-a",
            Status = ExternalProcessStatus.InvalidConfiguration,
            FileName = "worker-a.exe",
            Arguments = ["--port", "5050"],
            WorkingDirectory = "C:\\apps",
            LastError = validationError.Message,
            ValidationErrors = [validationError],
        };
        ExternalProcessManagerSnapshot managerSnapshot = new()
        {
            IsRunning = false,
            GeneratedAt = DateTimeOffset.UtcNow,
            Processes = [processSnapshot],
            ValidationErrors = [validationError],
        };

        Assert.False(managerSnapshot.IsRunning);
        Assert.Single(managerSnapshot.Processes);
        Assert.Single(managerSnapshot.ValidationErrors);
        Assert.Equal("worker-a", managerSnapshot.Processes[0].Alias);
        Assert.Equal(ExternalProcessStatus.InvalidConfiguration, managerSnapshot.Processes[0].Status);
        Assert.Equal("--port", managerSnapshot.Processes[0].Arguments[0]);
    }

    private sealed class TemporaryFile : IDisposable
    {
        private TemporaryFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryFile Create()
            => new(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.CreateVersion7():N}.tmp"));

        public async Task WaitUntilExists()
        {
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));

            while (timeout.IsCancellationRequested == false)
            {
                if (File.Exists(Path))
                    return;

                await Task.Delay(TimeSpan.FromMilliseconds(25), timeout.Token)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }

            throw new TimeoutException($"Temporary file '{Path}' was not created.");
        }

        public void Dispose()
        {
            try
            {
                File.Delete(Path);
            }
            catch (IOException)
            {
                // Temporary files are best-effort cleanup in tests.
            }
            catch (UnauthorizedAccessException)
            {
                // Temporary files are best-effort cleanup in tests.
            }
        }
    }
}
