using IvTem.ExternalProcessManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        await manager.StartAsync();
        ExternalProcessManagerSnapshot snapshot = manager.GetSnapshot();
        await manager.StopAsync();

        Assert.Same(services, returnedServices);
        Assert.True(snapshot.IsRunning);
        Assert.Empty(snapshot.Processes);
        Assert.Empty(snapshot.ValidationErrors);
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
}
