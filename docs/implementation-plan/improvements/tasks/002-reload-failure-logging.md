# Task 002: Log Hot-reload Background Failures

## Goal

Make unexpected failures in fire-and-forget configuration reload reconciliation
visible through `ILogger`.

## Required Reading

- `docs/configuration.md`
- `docs/host-integration.md`
- `docs/agent/async.md`
- `docs/agent/di-logging.md`
- `src/IvTem.ExternalProcessManager/ExternalProcessManager.cs`
- `tests/IvTem.ExternalProcessManager.Tests/ExternalProcessManagerTests.cs`

## Implementation Steps

1. Add a top-level exception guard around the background reload path.
2. Keep benign shutdown cases quiet:
   - `OperationCanceledException` caused by expected cancellation
   - `ObjectDisposedException` caused by manager disposal
3. Log any unexpected exception through a source-generated `LoggerMessage`
   method on `ExternalProcessManager`.
4. Include enough structured context to identify that the failure came from
   hot reload.
5. Add a focused unit test that triggers a reload failure from a fake
   supervisor or equivalent test seam.
6. Verify the test observes the log entry and that the exception does not crash
   the caller that raised the configuration reload.
7. Add a dated progress note to `../progress.md`.
8. Add a memory entry if the implementation discovers any reload-token or
   exception behavior worth preserving.

## Done Means

- Unexpected reload reconciliation failures are logged.
- Configuration reload remains best effort.
- Existing hot-reload behavior for valid and invalid entries is unchanged.
- The new behavior is covered by a unit test.

## Validation

Run:

```powershell
dotnet test IvTem.ExternalProcessManager.slnx
dotnet build IvTem.ExternalProcessManager.slnx
```

Verify:

- Existing hot-reload tests still pass.
- The new test fails before the fix and passes after the fix.
- The log message uses structured logging and does not include secrets.

## Stage And Commit

Run:

```powershell
git status --short
git add src/IvTem.ExternalProcessManager/ExternalProcessManager.cs tests/IvTem.ExternalProcessManager.Tests/ExternalProcessManagerTests.cs docs/implementation-plan/progress.md docs/implementation-plan/memory.md
git commit -m "Log hot reload background failures"
```

## Notes For Junior Developer

- Do not block the reload-token callback while trying to report the error.
- Do not add a separate file watcher.
- Avoid catching all exceptions deep in reconciliation logic; keep normal failure
  handling close to the fire-and-forget boundary.
