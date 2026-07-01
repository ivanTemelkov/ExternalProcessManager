# Task 003: Log Scheduled Timer Callback Failures

## Goal

Make unexpected scheduled restart timer callback failures visible through
`ILogger`.

## Required Reading

- `docs/scheduled-restarts.md`
- `docs/process-lifecycle.md`
- `docs/agent/async.md`
- `docs/agent/di-logging.md`
- `src/IvTem.ExternalProcessManager/Scheduling/SystemScheduledRestartTimer.cs`
- `src/IvTem.ExternalProcessManager/Scheduling/SystemScheduledRestartTimerFactory.cs`
- `tests/IvTem.ExternalProcessManager.Tests/Lifecycle/ExternalProcessSupervisorTests.cs`

## Implementation Steps

1. Add logging support to the production scheduled restart timer path.
2. Catch unexpected exceptions thrown by the timer callback.
3. Continue ignoring expected cancellation and disposal exceptions.
4. Use a source-generated `LoggerMessage` method.
5. Add a focused test for callback exception logging.
6. Add tests or update existing tests to confirm cancellation/disposal remains
   benign.
7. Add a dated progress note to `../progress.md`.
8. Add a memory entry if a timer ownership or logging decision is made.

## Done Means

- Timer callback failures are logged.
- Timer exceptions do not become unobserved task exceptions.
- Scheduled restart execution behavior remains unchanged for successful
  callbacks.
- The new behavior is tested.

## Validation

Run:

```powershell
dotnet test IvTem.ExternalProcessManager.slnx
dotnet build IvTem.ExternalProcessManager.slnx
```

Verify:

- Scheduled restart tests still pass.
- The new timer failure test observes the expected log entry.
- No direct console, file, EventLog, trace, or telemetry sink is added.

## Stage And Commit

Run:

```powershell
git status --short
git add src/IvTem.ExternalProcessManager/Scheduling tests/IvTem.ExternalProcessManager.Tests docs/implementation-plan/progress.md docs/implementation-plan/memory.md
git commit -m "Log scheduled timer callback failures"
```

## Notes For Junior Developer

- Keep timer logging internal.
- Do not expose timer events publicly.
- If constructor signatures change, update DI registration and tests in the
  same task.
