# Task 14: Hosted-Service Integration

## Goal

Support Microsoft.Extensions.Hosting integration through a service collection extension and hosted service.

## Implementation Steps

1. Implement `AddExternalProcessManager(IServiceCollection, IConfiguration)`.
2. Register the configuration section or config source used by the manager.
3. Register `IExternalProcessManager`.
4. Register an `IHostedService` that starts and stops the manager with the host.
5. Register internal services:
   - configuration reader
   - validator/normalizer
   - launcher
   - cleanup
   - scheduler/clock
6. Ensure hosted service owns lifecycle only in hosted mode.
7. Ensure manual consumers can still resolve `IExternalProcessManager`.

## Done Means

- Host apps can call `builder.Services.AddExternalProcessManager(builder.Configuration.GetSection("ExternalProcessManager"))`.
- The hosted service starts managed processes during host startup.
- The hosted service stops managed processes during host shutdown.
- The extension does not register duplicate managers unexpectedly.

## Test Plan

Unit tests:

- Build a `ServiceProvider` with the extension.
- Resolve `IExternalProcessManager`.
- Resolve hosted services and verify one manager hosted service exists.

Integration test:

- Start a generic host with helper process config.
- Verify process starts.
- Stop host.
- Verify process stops.

Run:

```powershell
dotnet test
```

## Notes For Junior Developer

- Use `ILogger` through DI.
- Do not write directly to console, files, EventLog, or telemetry systems.
