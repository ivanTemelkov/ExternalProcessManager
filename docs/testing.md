# Testing

## Test Projects

Planned test layout:

```text
tests/
  IvTem.ExternalProcessManager.Tests/
  IvTem.ExternalProcessManager.TestProcess/
```

`IvTem.ExternalProcessManager.Tests` contains unit and integration tests.

`IvTem.ExternalProcessManager.TestProcess` is a small executable used by integration tests to exercise real process behavior.

## Unit Tests

Configuration parsing:

- valid minimal process entry.
- duplicate aliases.
- missing alias.
- missing file name.
- `Arguments` only.
- `ArgumentList` only.
- both `Arguments` and `ArgumentList`, with `ArgumentList` preferred.
- environment variable parsing.
- working directory parsing.
- invalid restart mode.
- invalid duration.
- `MinBackoff` greater than `MaxBackoff`.

Day and schedule parsing:

- `All`.
- single day.
- comma-separated days.
- pipe-separated days.
- array days.
- invalid day.
- invalid hour.
- multiple schedules.

Scheduler:

- next run later today.
- next run on a later weekday.
- next run rolls to next week.
- duplicate schedules collapse into one due restart.

Reconciliation:

- added valid alias starts.
- removed alias stops.
- changed alias restarts.
- unchanged alias is preserved.
- invalid new alias is skipped.
- invalid changed alias keeps last valid config.

Backoff:

- first failure uses minimum delay.
- repeated failures grow exponentially.
- maximum delay is capped.
- stable runtime resets backoff.

## Integration Test Helper

The helper executable should support command-line modes such as:

- run until killed.
- exit immediately with configured exit code.
- delay then exit with configured exit code.
- spawn a child process.
- handle CTRL+BREAK and exit cleanly.
- ignore CTRL+BREAK to test forced kill.

The helper should write simple observable markers if tests need to verify behavior, but tests should prefer process state and exit codes over fragile text matching.

## Integration Tests

Process lifecycle:

- manager starts configured process.
- process ID appears in diagnostics.
- manager stops process on shutdown.
- process tree is killed after graceful timeout.

Restart behavior:

- default `NonZeroExitCode` does not restart exit code `0`.
- default `NonZeroExitCode` restarts nonzero exit.
- `Always` restarts exit code `0`.
- `Never` does not restart nonzero exit.

Scheduled restarts:

- due schedule restarts running process.
- diagnostics updates next scheduled restart.
- schedule change through hot reload is applied.

Hot reload:

- adding a process starts it.
- removing a process stops it.
- changing launch config restarts it.
- invalid new config is visible in diagnostics.
- invalid changed config keeps existing process running.

## Build Verification

Required commands:

```powershell
dotnet build
dotnet test
```

AOT and trim analyzer warnings must be resolved before considering the implementation complete.
