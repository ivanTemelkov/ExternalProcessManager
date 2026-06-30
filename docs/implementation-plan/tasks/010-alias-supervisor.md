# Task 10: Per-Alias Supervisor

## Goal

Implement one supervisor object per configured alias to serialize lifecycle operations and restart decisions.

## Implementation Steps

1. Add an internal supervisor for one effective process config.
2. Serialize operations for the alias using an async lock or command loop.
3. Implement start.
4. Implement intentional stop.
5. Observe process exit.
6. Classify exits as intentional or non-intentional.
7. Apply restart modes:
   - `NonZeroExitCode`
   - `Always`
   - `Never`
8. Apply backoff before restart.
9. Track runtime state:
   - status
   - process ID
   - start time
   - exit time
   - last exit code
   - restart count
   - last error

## Done Means

- One alias cannot start duplicate processes due to racing lifecycle events.
- Non-intentional exits restart only when policy requires it.
- Intentional stops do not restart.
- Supervisor exposes state needed by diagnostics.

## Test Plan

Unit tests with fake launcher, fake cleanup, and fake clock:

- Start transitions to running.
- Stop transitions to stopped.
- Exit code `0` with default mode stays stopped.
- Nonzero exit with default mode restarts.
- `Always` restarts exit code `0`.
- `Never` does not restart nonzero exit.
- Intentional stop does not restart.
- Backoff delay is requested before restart.

Integration tests:

- Helper exits with `0`.
- Helper exits with nonzero.
- Helper is killed externally and restarts when policy allows.

Run:

```powershell
dotnet test
```

## Notes For Junior Developer

- Keep manager-level hot reload out of this task.
- The supervisor should consume effective config only.
