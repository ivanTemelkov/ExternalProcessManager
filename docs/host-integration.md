# Host Integration

## Supported Integration Models

The library supports two host integration styles:

- DI and hosted service integration for Generic Host, Worker Service, ASP.NET Core, and similar hosts.
- Manual manager construction and lifecycle control for applications that do not use Microsoft.Extensions.Hosting.

## DI Hosted Service API

The primary host integration should look like this:

```csharp
builder.Services.AddExternalProcessManager(
    builder.Configuration.GetSection("ExternalProcessManager"));
```

The extension method registers:

- configuration binding and validation services
- `IExternalProcessManager`
- hosted service that starts and stops the manager with the host
- process launching and Windows cleanup services
- diagnostics snapshot provider
- scheduler and clock abstractions

The hosted service owns manager lifecycle:

- starts configured processes during host startup
- applies hot reload while running
- stops all managed processes during host shutdown

## Manual API

Manual consumers should be able to construct or resolve an `IExternalProcessManager` and control it directly:

```csharp
await manager.StartAsync(cancellationToken);

ExternalProcessManagerSnapshot snapshot = manager.GetSnapshot();

await manager.StopAsync(cancellationToken);
```

The manual API must be idempotent:

- repeated `StartAsync` calls should not start duplicate processes
- repeated `StopAsync` calls should be safe
- `GetSnapshot` should be safe before start, while running, and after stop

## Public Abstractions

The public API should stay compact:

- `IExternalProcessManager`
- `ExternalProcessManagerSnapshot`
- process diagnostics record types
- configuration option record types only where hosts need strongly typed access
- service collection extension methods

Internal implementation details should stay internal:

- process launcher
- reconciliation engine
- scheduler
- backoff policy
- Windows process control APIs

## Lifecycle Ownership

The library owns only processes configured through its own configuration section. It must not discover or manage unrelated OS processes.

The host owns:

- configuration source setup
- logging configuration
- application shutdown timeout
- calling manual lifecycle methods if not using hosted service mode

The library owns:

- starting configured processes
- tracking process identity by alias
- restart decisions
- scheduled restarts
- cleanup of managed process trees
- diagnostics snapshots

## Logging

The library logs through `ILogger`.

Expected log categories:

- manager start and stop
- process start and exit
- restart decisions and backoff delays
- scheduled restart execution
- configuration hot reload
- validation errors
- graceful shutdown and forced kill results

The library should not write directly to console, files, EventLog, or telemetry systems.
