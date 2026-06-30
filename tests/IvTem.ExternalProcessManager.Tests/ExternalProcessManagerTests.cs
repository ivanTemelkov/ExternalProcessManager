using IvTem.ExternalProcessManager.Configuration;
using IvTem.ExternalProcessManager.Lifecycle;
using Microsoft.Extensions.Configuration;

namespace IvTem.ExternalProcessManager.Tests;

public sealed class ExternalProcessManagerTests
{
    [Fact]
    public void GetSnapshotBeforeStartIsValid()
    {
        TestConfigurationSource source = new(CreateConfiguration(("worker-a", "worker-a.exe")));
        IConfigurationRoot configuration = BuildConfiguration(source);
        FakeSupervisorFactory supervisorFactory = new();
        using ExternalProcessManager manager = CreateManager(configuration, supervisorFactory);

        ExternalProcessManagerSnapshot snapshot = manager.GetSnapshot();

        Assert.False(snapshot.IsRunning);
        Assert.Empty(snapshot.Processes);
        Assert.Empty(snapshot.ValidationErrors);
    }

    [Fact]
    public async Task StartStartsValidAliases()
    {
        TestConfigurationSource source = new(CreateConfiguration(("worker-a", "worker-a.exe")));
        IConfigurationRoot configuration = BuildConfiguration(source);
        FakeSupervisorFactory supervisorFactory = new();
        using ExternalProcessManager manager = CreateManager(configuration, supervisorFactory);

        await manager.StartAsync();

        FakeSupervisor supervisor = Assert.Single(supervisorFactory.Supervisors);
        Assert.Equal("worker-a", supervisor.Configuration.Alias);
        Assert.Equal(1, supervisor.StartCount);
        Assert.Equal(ExternalProcessStatus.Running, manager.GetSnapshot().Processes[0].Status);
    }

    [Fact]
    public async Task StopKeepsStoppedProcessDiagnostics()
    {
        TestConfigurationSource source = new(CreateConfiguration(("worker-a", "worker-a.exe")));
        IConfigurationRoot configuration = BuildConfiguration(source);
        FakeSupervisorFactory supervisorFactory = new();
        using ExternalProcessManager manager = CreateManager(configuration, supervisorFactory);
        await manager.StartAsync();

        await manager.StopAsync();

        ExternalProcessManagerSnapshot snapshot = manager.GetSnapshot();
        ExternalProcessSnapshot process = Assert.Single(snapshot.Processes);
        Assert.False(snapshot.IsRunning);
        Assert.Equal("worker-a", process.Alias);
        Assert.Equal(ExternalProcessStatus.Stopped, process.Status);
    }

    [Fact]
    public async Task StartAfterStopStartsExistingSupervisors()
    {
        TestConfigurationSource source = new(CreateConfiguration(("worker-a", "worker-a.exe")));
        IConfigurationRoot configuration = BuildConfiguration(source);
        FakeSupervisorFactory supervisorFactory = new();
        using ExternalProcessManager manager = CreateManager(configuration, supervisorFactory);
        await manager.StartAsync();
        await manager.StopAsync();
        FakeSupervisor supervisor = Assert.Single(supervisorFactory.Supervisors);

        await manager.StartAsync();

        Assert.Single(supervisorFactory.Supervisors);
        Assert.Equal(2, supervisor.StartCount);
        Assert.Equal(ExternalProcessStatus.Running, manager.GetSnapshot().Processes[0].Status);
    }

    [Fact]
    public async Task ReloadAddedValidAliasStartsSupervisor()
    {
        TestConfigurationSource source = new(CreateConfiguration(("worker-a", "worker-a.exe")));
        IConfigurationRoot configuration = BuildConfiguration(source);
        FakeSupervisorFactory supervisorFactory = new();
        using ExternalProcessManager manager = CreateManager(configuration, supervisorFactory);
        await manager.StartAsync();

        source.Replace(CreateConfiguration(("worker-a", "worker-a.exe"), ("worker-b", "worker-b.exe")));

        await WaitUntil(() => supervisorFactory.Supervisors.Count == 2);

        Assert.Contains(supervisorFactory.Supervisors, supervisor => supervisor.Configuration.Alias == "worker-b");
        Assert.Equal(2, manager.GetSnapshot().Processes.Length);
    }

    [Fact]
    public async Task ReloadRemovedAliasStopsAndRemovesSupervisor()
    {
        TestConfigurationSource source = new(CreateConfiguration(("worker-a", "worker-a.exe"), ("worker-b", "worker-b.exe")));
        IConfigurationRoot configuration = BuildConfiguration(source);
        FakeSupervisorFactory supervisorFactory = new();
        using ExternalProcessManager manager = CreateManager(configuration, supervisorFactory);
        await manager.StartAsync();
        FakeSupervisor removedSupervisor = supervisorFactory.Supervisors.Single(supervisor => supervisor.Configuration.Alias == "worker-b");

        source.Replace(CreateConfiguration(("worker-a", "worker-a.exe")));

        await WaitUntil(() => removedSupervisor.StopCount == 1);

        ExternalProcessManagerSnapshot snapshot = manager.GetSnapshot();
        Assert.Single(snapshot.Processes);
        Assert.Equal("worker-a", snapshot.Processes[0].Alias);
        Assert.True(removedSupervisor.IsDisposed);
    }

    [Fact]
    public async Task ReloadChangedAliasRestartsWithNewConfiguration()
    {
        TestConfigurationSource source = new(CreateConfiguration(("worker-a", "worker-a.exe")));
        IConfigurationRoot configuration = BuildConfiguration(source);
        FakeSupervisorFactory supervisorFactory = new();
        using ExternalProcessManager manager = CreateManager(configuration, supervisorFactory);
        await manager.StartAsync();
        FakeSupervisor originalSupervisor = supervisorFactory.Supervisors[0];

        source.Replace(CreateConfiguration(("worker-a", "worker-a-v2.exe")));

        await WaitUntil(() => supervisorFactory.Supervisors.Count == 2);

        FakeSupervisor replacementSupervisor = supervisorFactory.Supervisors[1];
        Assert.Equal(1, originalSupervisor.StopCount);
        Assert.True(originalSupervisor.IsDisposed);
        Assert.Equal("worker-a-v2.exe", replacementSupervisor.Configuration.FileName);
        Assert.Equal("worker-a-v2.exe", manager.GetSnapshot().Processes[0].FileName);
    }

    [Fact]
    public async Task ReloadUnchangedAliasPreservesSupervisor()
    {
        TestConfigurationSource source = new(CreateConfiguration(("worker-a", "worker-a.exe")));
        IConfigurationRoot configuration = BuildConfiguration(source);
        FakeSupervisorFactory supervisorFactory = new();
        using ExternalProcessManager manager = CreateManager(configuration, supervisorFactory);
        await manager.StartAsync();
        FakeSupervisor originalSupervisor = supervisorFactory.Supervisors[0];

        source.Replace(CreateConfiguration(("worker-a", "worker-a.exe")));

        await Task.Delay(TimeSpan.FromMilliseconds(50));

        Assert.Single(supervisorFactory.Supervisors);
        Assert.Equal(0, originalSupervisor.StopCount);
        Assert.Equal(1, originalSupervisor.StartCount);
    }

    [Fact]
    public async Task ReloadInvalidNewAliasIsSkippedAndReported()
    {
        TestConfigurationSource source = new(CreateConfiguration(("worker-a", "worker-a.exe")));
        IConfigurationRoot configuration = BuildConfiguration(source);
        FakeSupervisorFactory supervisorFactory = new();
        using ExternalProcessManager manager = CreateManager(configuration, supervisorFactory);
        await manager.StartAsync();

        source.Replace(CreateConfiguration(("worker-a", "worker-a.exe"), ("worker-b", null)));

        await WaitUntil(() => manager.GetSnapshot().ValidationErrors.Length == 1);

        ExternalProcessManagerSnapshot snapshot = manager.GetSnapshot();
        Assert.Single(supervisorFactory.Supervisors);
        Assert.Equal(2, snapshot.Processes.Length);
        Assert.Contains(snapshot.Processes, process => process.Alias == "worker-b"
            && process.Status == ExternalProcessStatus.InvalidConfiguration);
    }

    [Fact]
    public async Task ReloadInvalidChangedAliasKeepsExistingSupervisorRunning()
    {
        TestConfigurationSource source = new(CreateConfiguration(("worker-a", "worker-a.exe")));
        IConfigurationRoot configuration = BuildConfiguration(source);
        FakeSupervisorFactory supervisorFactory = new();
        using ExternalProcessManager manager = CreateManager(configuration, supervisorFactory);
        await manager.StartAsync();
        FakeSupervisor originalSupervisor = supervisorFactory.Supervisors[0];

        source.Replace(CreateConfiguration(("worker-a", null)));

        await WaitUntil(() => manager.GetSnapshot().ValidationErrors.Length == 1);

        ExternalProcessSnapshot snapshot = Assert.Single(manager.GetSnapshot().Processes);
        Assert.Single(supervisorFactory.Supervisors);
        Assert.Equal(0, originalSupervisor.StopCount);
        Assert.Equal(ExternalProcessStatus.Running, snapshot.Status);
        Assert.Equal("worker-a.exe", snapshot.FileName);
        Assert.Single(snapshot.ValidationErrors);
    }

    [Fact]
    public async Task GetSnapshotReflectsSupervisorStateChanges()
    {
        TestConfigurationSource source = new(CreateConfiguration(("worker-a", "worker-a.exe")));
        IConfigurationRoot configuration = BuildConfiguration(source);
        FakeSupervisorFactory supervisorFactory = new();
        using ExternalProcessManager manager = CreateManager(configuration, supervisorFactory);
        await manager.StartAsync();
        FakeSupervisor supervisor = Assert.Single(supervisorFactory.Supervisors);

        supervisor.SetStatus(ExternalProcessStatus.RestartPending);

        ExternalProcessSnapshot snapshot = Assert.Single(manager.GetSnapshot().Processes);
        Assert.Equal(ExternalProcessStatus.RestartPending, snapshot.Status);
    }

    [Fact]
    public async Task ConcurrentSnapshotCallsDoNotThrowDuringLifecycleOperations()
    {
        TestConfigurationSource source = new(CreateConfiguration(("worker-a", "worker-a.exe"), ("worker-b", "worker-b.exe")));
        IConfigurationRoot configuration = BuildConfiguration(source);
        FakeSupervisorFactory supervisorFactory = new();
        using ExternalProcessManager manager = CreateManager(configuration, supervisorFactory);
        await manager.StartAsync();

        Task snapshotReads = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                _ = manager.GetSnapshot();
            }
        });

        await Task.WhenAll(manager.StopAsync(), snapshotReads);

        Assert.False(manager.GetSnapshot().IsRunning);
    }

    [Fact]
    public async Task SnapshotCollectionsCannotBeMutatedByCallers()
    {
        TestConfigurationSource source = new(CreateConfiguration(("worker-a", null)));
        IConfigurationRoot configuration = BuildConfiguration(source);
        FakeSupervisorFactory supervisorFactory = new();
        using ExternalProcessManager manager = CreateManager(configuration, supervisorFactory);

        await manager.StartAsync();

        ExternalProcessManagerSnapshot snapshot = manager.GetSnapshot();
        Assert.Throws<NotSupportedException>(() => ((IList<ExternalProcessSnapshot>)snapshot.Processes).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<ExternalProcessValidationError>)snapshot.ValidationErrors).Clear());
    }

    [Fact]
    public async Task ReloadAppliesMultipleChangesBestEffort()
    {
        TestConfigurationSource source = new(CreateConfiguration(("worker-a", "worker-a.exe")));
        IConfigurationRoot configuration = BuildConfiguration(source);
        FakeSupervisorFactory supervisorFactory = new();
        using ExternalProcessManager manager = CreateManager(configuration, supervisorFactory);
        await manager.StartAsync();
        FakeSupervisor originalSupervisor = supervisorFactory.Supervisors[0];

        source.Replace(CreateConfiguration(("worker-a", "worker-a.exe"), ("worker-b", "worker-b.exe"), ("worker-c", null)));

        await WaitUntil(() => supervisorFactory.Supervisors.Count == 2
            && manager.GetSnapshot().ValidationErrors.Length == 1);

        Assert.Equal(0, originalSupervisor.StopCount);
        Assert.Contains(supervisorFactory.Supervisors, supervisor => supervisor.Configuration.Alias == "worker-b");
        Assert.Contains(manager.GetSnapshot().Processes, process => process.Alias == "worker-c"
            && process.Status == ExternalProcessStatus.InvalidConfiguration);
    }

    private static ExternalProcessManager CreateManager(
        IConfiguration configuration,
        IExternalProcessSupervisorFactory supervisorFactory)
        => new(
            new ExternalProcessManagerConfigurationSource(configuration.GetSection("ExternalProcessManager")),
            new ExternalProcessConfigurationReader(),
            new ExternalProcessConfigurationValidator(),
            supervisorFactory);

    private static IConfigurationRoot BuildConfiguration(TestConfigurationSource source)
        => new ConfigurationBuilder()
            .Add(source)
            .Build();

    private static Dictionary<string, string?> CreateConfiguration(params (string Alias, string? FileName)[] processes)
    {
        Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < processes.Length; i++)
        {
            values[$"ExternalProcessManager:Processes:{i}:Alias"] = processes[i].Alias;

            if (processes[i].FileName is not null)
                values[$"ExternalProcessManager:Processes:{i}:FileName"] = processes[i].FileName;
        }

        return values;
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(5);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (condition())
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }

        Assert.True(condition(), "Condition was not satisfied before the timeout.");
    }

    private sealed class TestConfigurationSource : IConfigurationSource
    {
        public TestConfigurationSource(Dictionary<string, string?> values)
        {
            Provider = new TestConfigurationProvider(values);
        }

        private TestConfigurationProvider Provider { get; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
            => Provider;

        public void Replace(Dictionary<string, string?> values)
            => Provider.Replace(values);
    }

    private sealed class TestConfigurationProvider : ConfigurationProvider
    {
        public TestConfigurationProvider(Dictionary<string, string?> values)
        {
            ReplaceValues(values);
        }

        public void Replace(Dictionary<string, string?> values)
        {
            ReplaceValues(values);
            OnReload();
        }

        private void ReplaceValues(Dictionary<string, string?> values)
        {
            Data.Clear();

            foreach (KeyValuePair<string, string?> item in values)
            {
                if (item.Value is not null)
                    Data[item.Key] = item.Value;
            }
        }
    }

    private sealed class FakeSupervisorFactory : IExternalProcessSupervisorFactory
    {
        public List<FakeSupervisor> Supervisors { get; } = [];

        public IExternalProcessSupervisor Create(EffectiveExternalProcessConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            FakeSupervisor supervisor = new(configuration);
            Supervisors.Add(supervisor);
            return supervisor;
        }
    }

    private sealed class FakeSupervisor : IExternalProcessSupervisor
    {
        public FakeSupervisor(EffectiveExternalProcessConfiguration configuration)
        {
            Configuration = configuration;
            Snapshot = CreateSnapshot(ExternalProcessStatus.NotStarted);
        }

        public EffectiveExternalProcessConfiguration Configuration { get; }

        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public bool IsDisposed { get; private set; }

        private ExternalProcessSnapshot Snapshot { get; set; }

        public ExternalProcessSnapshot GetSnapshot()
            => Snapshot;

        public Task Start(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            StartCount++;
            Snapshot = CreateSnapshot(ExternalProcessStatus.Running);
            return Task.CompletedTask;
        }

        public Task Stop(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            StopCount++;
            Snapshot = CreateSnapshot(ExternalProcessStatus.Stopped);
            return Task.CompletedTask;
        }

        public void Dispose()
            => IsDisposed = true;

        public void SetStatus(ExternalProcessStatus status)
            => Snapshot = CreateSnapshot(status);

        private ExternalProcessSnapshot CreateSnapshot(ExternalProcessStatus status)
            => new()
            {
                Alias = Configuration.Alias,
                Status = status,
                FileName = Configuration.FileName,
                Arguments = Configuration.ArgumentList,
                WorkingDirectory = Configuration.WorkingDirectory,
            };
    }
}
