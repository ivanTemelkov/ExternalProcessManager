# Task 16: Integration Test Helper Executable

## Goal

Create a small executable used by integration tests to exercise real process behavior.

## Implementation Steps

1. Implement command-line modes:
   - run until killed
   - exit immediately with configured exit code
   - delay then exit with configured exit code
   - spawn a child process
   - handle CTRL+BREAK and exit cleanly
   - ignore CTRL+BREAK
2. Keep output simple and optional.
3. Prefer observable process state and exit codes over fragile text matching.
4. Add helper options for marker files only when process state is not enough.
5. Document helper arguments in the helper source or README.

## Done Means

- Integration tests can start helper in every required mode.
- Helper exits predictably.
- Helper can simulate graceful and forced cleanup paths.
- Helper can simulate process tree cleanup.

## Test Plan

Helper-specific integration tests:

- Immediate exit returns requested exit code.
- Delayed exit returns requested exit code.
- Run-until-killed remains alive until terminated.
- Child process mode creates a descendant process.
- CTRL+BREAK handling mode exits cleanly.
- Ignore mode requires forced termination.

Run:

```powershell
dotnet test
```

## Notes For Junior Developer

- Keep helper behavior deterministic and small.
- Avoid sleeps longer than necessary; tests should stay fast.
