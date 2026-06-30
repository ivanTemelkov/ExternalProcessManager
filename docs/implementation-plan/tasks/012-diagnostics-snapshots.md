# Task 12: Diagnostics Snapshots

## Goal

Expose immutable, thread-safe snapshots through `GetSnapshot()`.

## Implementation Steps

1. Maintain the latest snapshot in an atomically replaceable field.
2. Include manager-level fields:
   - `IsRunning`
   - `GeneratedAt`
   - `Processes`
   - `ValidationErrors`
3. Include process fields:
   - alias
   - status
   - file name
   - arguments
   - working directory
   - process ID
   - started at
   - exited at
   - last exit code
   - restart count
   - next scheduled restart
   - last error
   - validation errors
4. Show effective configuration values, including defaults.
5. Include invalid configured aliases when possible.
6. Ensure returned snapshots can be read safely without internal locks.

## Done Means

- `GetSnapshot()` is safe before start, while running, and after stop.
- Snapshot collections cannot be mutated by callers.
- Invalid config appears in diagnostics.
- Snapshot does not depend on logs.

## Test Plan

Unit tests:

- Snapshot before start is valid.
- Snapshot while running includes process info.
- Snapshot after stop updates status.
- Invalid alias appears with validation errors.
- Changed invalid config shows last valid effective config plus errors.
- Concurrent snapshot calls do not throw while lifecycle operations run.

Run:

```powershell
dotnet test
```

## Notes For Junior Developer

- Do not block `GetSnapshot()` on long process cleanup.
- Prefer replacing whole immutable snapshots over mutating shared lists.
