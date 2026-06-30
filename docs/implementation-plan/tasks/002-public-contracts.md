# Task 02: Define Public Contracts

## Goal

Create the compact public API that hosts will use.

## Implementation Steps

1. Add `IExternalProcessManager` with:
   - `Task StartAsync(CancellationToken cancellationToken = default)`
   - `Task StopAsync(CancellationToken cancellationToken = default)`
   - `ExternalProcessManagerSnapshot GetSnapshot()`
2. Add immutable diagnostics records:
   - `ExternalProcessManagerSnapshot`
   - `ExternalProcessSnapshot`
   - `ExternalProcessValidationError`
3. Add a public process status enum. Suggested values:
   - `NotStarted`
   - `Starting`
   - `Running`
   - `Stopping`
   - `RestartPending`
   - `Stopped`
   - `Faulted`
   - `InvalidConfiguration`
4. Add public service registration extension:
   - `AddExternalProcessManager(this IServiceCollection services, IConfiguration section)`
5. Keep implementation details internal.
6. Add XML documentation comments where names are not self-explanatory.

## Done Means

- Public contracts compile.
- Public API exposes only the planned types.
- Snapshot records are immutable after construction.
- The service extension exists, even if its implementation is temporary until later tasks.

## Test Plan

Add compile-oriented unit tests that:

- Reference `IExternalProcessManager`.
- Construct snapshot records.
- Call the service collection extension in a minimal `ServiceCollection`.

Run:

```powershell
dotnet test
```

Expected result:

- Public API is usable from the test assembly.

## Notes For Junior Developer

- Do not expose launcher, scheduler, cleanup, validation engine, or supervisor types publicly.
- Prefer `IReadOnlyList<T>` or immutable arrays for snapshot collections.
