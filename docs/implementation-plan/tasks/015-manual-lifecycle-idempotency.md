# Task 15: Manual Lifecycle Idempotency And Disposal

## Goal

Make direct use of `IExternalProcessManager` safe and repeatable.

## Implementation Steps

1. Make `StartAsync` idempotent.
2. Make `StopAsync` idempotent.
3. Make `GetSnapshot` safe before start, while running, and after stop.
4. Ensure repeated `StartAsync` calls do not start duplicate processes.
5. Ensure repeated `StopAsync` calls do not throw.
6. Dispose:
   - process handles
   - timers
   - cancellation token sources
   - change subscriptions
7. Decide whether manager implements `IAsyncDisposable`, `IDisposable`, or both if needed internally.

## Done Means

- Manual lifecycle can be used without hosted service.
- Repeated start/stop calls are safe.
- Disposal leaves no running managed child processes.
- Snapshot remains safe after stop.

## Test Plan

Unit tests:

- `GetSnapshot` before start returns valid stopped snapshot.
- Calling `StartAsync` twice starts each alias once.
- Calling `StopAsync` twice succeeds.
- Calling start after stop follows the intended lifecycle behavior.

Integration tests:

- Start helper process manually.
- Stop manager and verify helper exits.
- Dispose manager and verify cleanup.

Run:

```powershell
dotnet test
```

## Notes For Junior Developer

- Do not rely on finalizers for cleanup.
- Keep lifecycle state transitions explicit.
