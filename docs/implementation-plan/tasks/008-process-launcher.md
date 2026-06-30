# Task 08: Process Launcher Abstraction

## Goal

Start external processes through an internal abstraction that can be faked in tests.

## Implementation Steps

1. Add an internal process launcher interface.
2. Implement a Windows process launcher using `ProcessStartInfo`.
3. Map effective config to `ProcessStartInfo`:
   - file name
   - raw arguments when structured arguments are absent
   - `ArgumentList` when structured arguments are present
   - working directory
   - environment overrides
4. Enable exit observation.
5. Return a process handle abstraction containing:
   - process ID
   - start time
   - exit task or callback
6. Ensure all handles are disposable.

## Done Means

- Runtime code does not directly construct `Process` except inside launcher implementation.
- Tests can use a fake launcher.
- Diagnostics can obtain process ID and start time.

## Test Plan

Unit tests with fake or inspectable launcher:

- File name maps correctly.
- Raw arguments map correctly.
- Structured arguments map correctly.
- Structured arguments win over raw arguments.
- Working directory maps correctly.
- Environment overrides map correctly.

Integration test:

- Start the helper process.
- Verify diagnostics show a process ID.

Run:

```powershell
dotnet test
```

## Notes For Junior Developer

- Keep Windows-specific behavior internal.
- Do not implement restart policy here.
