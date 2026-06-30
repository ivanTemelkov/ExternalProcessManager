# Task 19: Sample Host Application

## Goal

Add runnable sample applications that show how a host uses the library through
Microsoft.Extensions.Hosting, configuration, hosted-service lifecycle, and
diagnostics snapshots.

## Implementation Steps

1. Add a `samples/` solution folder.
2. Add `samples/IvTem.ExternalProcessManager.SampleHost` as a `net10.0`
   console application.
3. Add `samples/IvTem.ExternalProcessManager.SampleWorker` as a tiny
   `net10.0` console executable supervised by the sample host.
4. Reference `src/IvTem.ExternalProcessManager` from the sample host.
5. Configure the sample host with `Host.CreateApplicationBuilder`.
6. Register the library with:

   ```csharp
   builder.Services.AddExternalProcessManager(
       builder.Configuration.GetSection("ExternalProcessManager"));
   ```

7. Use `appsettings.json` for sample process settings such as alias,
   restart policy, graceful timeout, environment variables, and scheduled
   restarts.
8. Layer a sample-only in-memory configuration override for the worker
   executable path and argument list so the sample runs from the repository
   without hard-coded absolute paths.
9. Add a sample background service that periodically reads
   `IExternalProcessManager.GetSnapshot()` and logs manager state, process
   alias, status, process ID, restart count, next scheduled restart, and
   validation errors.
10. Make the sample host exit clearly on non-Windows because the library's
    process-control behavior is Windows-only.
11. Add a finite-run option such as `SampleHost:RunSeconds` so the sample can be
    smoke-tested without manual interruption.
12. Add both sample projects to `IvTem.ExternalProcessManager.slnx`.

## Done Means

- A user can run the sample host from the repository.
- The sample host starts the sample worker through
  `AddExternalProcessManager`.
- The hosted service starts and stops the managed worker with the host.
- Diagnostics snapshots are visible through structured logs.
- The sample is self-contained and does not depend on the test helper
  executable.
- The library public API remains unchanged.

## Test Plan

Run:

```powershell
dotnet build IvTem.ExternalProcessManager.slnx
dotnet test IvTem.ExternalProcessManager.slnx
```

On Windows, run:

```powershell
dotnet run --project samples/IvTem.ExternalProcessManager.SampleHost -- --SampleHost:RunSeconds=8
```

Verify:

- Build and tests stay warning-clean.
- The sample worker starts and appears in diagnostics as `Running`.
- The sample logs include the configured alias, process ID, restart count, and
  next scheduled restart value when configured.
- Timed host shutdown stops the worker cleanly.

## Notes For Junior Developer

- Keep the sample focused on host usage, not internal library behavior.
- Do not reuse the integration test helper as the public sample worker.
- Do not add a separate configuration watcher; use the host configuration flow.
- Keep sample-only path discovery in the sample host, not in the library.
