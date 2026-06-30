# Task 09: Windows Cleanup Abstraction

## Goal

Stop managed processes gracefully when possible and force-kill the process tree when required.

## Implementation Steps

1. Add an internal process cleanup interface.
2. Implement Windows cleanup behind that interface.
3. Attempt console control graceful stop, such as CTRL+BREAK, when possible.
4. Wait for `GracefulStopTimeout`.
5. If the process is still alive, kill the process tree.
6. Report the cleanup outcome to the caller.
7. Keep P/Invoke declarations internal and minimal.

## Done Means

- Cleanup can stop the root process and descendants.
- Public APIs do not expose Windows interop details.
- Cleanup reports whether graceful stop succeeded or force kill was needed.

## Test Plan

Integration tests with helper process:

- Helper that handles CTRL+BREAK exits cleanly before timeout.
- Helper that ignores CTRL+BREAK is killed after timeout.
- Helper that spawns a child has its child process terminated.

Run:

```powershell
dotnet test
```

## Notes For Junior Developer

- This task is Windows-specific.
- If process-group console control is unreliable in the test environment, record the limitation in `../memory.md` and keep a fallback tree kill path.
