# Task 004: Harden Stop Cancellation State

## Goal

Avoid publishing a fully stopped manager state when `StopAsync` is canceled
before all supervised aliases are cleaned up.

## Required Reading

- `docs/process-lifecycle.md`
- `docs/diagnostics.md`
- `docs/host-integration.md`
- `docs/agent/async.md`
- `src/IvTem.ExternalProcessManager/ExternalProcessManager.cs`
- `src/IvTem.ExternalProcessManager/Lifecycle/ExternalProcessSupervisor.cs`
- `tests/IvTem.ExternalProcessManager.Tests/ExternalProcessManagerTests.cs`

## Implementation Steps

1. Review current manager stop ordering and snapshot updates.
2. Ensure `IsRunning` is not set to `false` until all supervisors stop
   successfully.
3. If stop is canceled, refresh diagnostics and rethrow cancellation.
4. Ensure a later `StopAsync` call with a fresh token can retry cleanup.
5. Add a unit test with multiple fake supervisors where one stop is canceled.
6. Assert that:
   - cleanup stops before later aliases only if cancellation requires it.
   - manager diagnostics do not claim a completed stop.
   - a later stop can complete.
7. Add a source-generated log for incomplete/canceled stop if useful.
8. Add a dated progress note to `../progress.md`.
9. Add a memory entry describing the final cancellation semantics.

## Done Means

- Stop cancellation has deterministic diagnostics.
- Partial cleanup is not presented as a completed manager stop.
- Retry after cancellation is tested.
- Existing idempotent start/stop behavior still works.

## Validation

Run:

```powershell
dotnet test IvTem.ExternalProcessManager.slnx
dotnet build IvTem.ExternalProcessManager.slnx
```

Verify:

- Manual lifecycle tests pass.
- Hosted-service integration tests pass.
- New cancellation tests cover both canceled stop and successful retry.

## Stage And Commit

Run:

```powershell
git status --short
git add src/IvTem.ExternalProcessManager/ExternalProcessManager.cs tests/IvTem.ExternalProcessManager.Tests/ExternalProcessManagerTests.cs docs/implementation-plan/progress.md docs/implementation-plan/memory.md
git commit -m "Harden stop cancellation state"
```

## Notes For Junior Developer

- Do not swallow cancellation; callers need to know shutdown was interrupted.
- Do not dispose supervisors after a canceled stop unless the manager itself is
  being disposed.
- Keep `GetSnapshot()` low-cost and thread-safe.
