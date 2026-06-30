# Task 13: Scheduled Restart Execution

## Goal

Wire scheduler calculations into runtime execution so configured processes restart at local maintenance times.

## Implementation Steps

1. Add per-supervisor or per-manager timer scheduling.
2. Use the injectable clock and scheduler calculation to compute next due time.
3. Store next scheduled restart in diagnostics.
4. At due time:
   - mark restart as intentional
   - gracefully stop process
   - force-kill if needed
   - start process again with current valid effective config
5. If the process is already stopped but still supervised, start it and log that no running process was found.
6. After execution, calculate the next occurrence again.
7. When schedules change through hot reload, refresh the timer and diagnostics.
8. Prevent duplicate restarts when multiple schedules resolve to the same time.

## Done Means

- Scheduled restarts occur without being treated as crash-loop exits.
- Diagnostics show next scheduled restart.
- Schedule-only changes refresh the scheduler.
- Duplicate due schedules cause one restart.

## Test Plan

Unit tests with fake clock/timer:

- Due schedule restarts running process.
- Next scheduled restart updates after execution.
- Duplicate due schedules cause one restart.
- Schedule change through hot reload updates next due time.
- Stopped supervised process is started at schedule time.

Integration test:

- Configure a near-future schedule for helper process.
- Verify process ID changes after scheduled restart.

Run:

```powershell
dotnet test
```

## Notes For Junior Developer

- Do not use real long waits in tests.
- Keep timer disposal strict to avoid tests hanging.
