# Task 006: Define Launch Failure Policy

## Goal

Make process launch failure behavior explicit and tested.

## Required Reading

- `docs/process-lifecycle.md`
- `docs/diagnostics.md`
- `docs/configuration.md`
- `src/IvTem.ExternalProcessManager/Lifecycle/ExternalProcessSupervisor.cs`
- `tests/IvTem.ExternalProcessManager.Tests/Lifecycle/ExternalProcessSupervisorTests.cs`

## Implementation Steps

1. Keep the current v1 behavior: a launch failure moves the alias to `Faulted`
   and does not automatically retry through restart backoff.
2. Update `docs/process-lifecycle.md` with a short `Launch Failure` section.
3. State that retrying launch failures is out of scope for v1 unless a later
   task changes the policy.
4. Add or confirm a test that launch failure:
   - sets status to `Faulted`
   - sets `LastError`
   - does not increment `RestartCount`
   - does not request a restart delay
5. Add a dated progress note to `../progress.md`.
6. Add a memory entry documenting the v1 launch failure policy.

## Done Means

- Launch failure behavior is documented.
- Launch failure behavior is tested.
- Host developers can distinguish launch failure from process exit restart
  behavior.

## Validation

Run:

```powershell
dotnet test IvTem.ExternalProcessManager.slnx
dotnet build IvTem.ExternalProcessManager.slnx
```

Verify:

- The launch failure test covers diagnostics and no-retry behavior.
- `docs/process-lifecycle.md` clearly distinguishes launch failure from
  nonzero process exit.

## Stage And Commit

Run:

```powershell
git status --short
git add docs/process-lifecycle.md tests/IvTem.ExternalProcessManager.Tests/Lifecycle/ExternalProcessSupervisorTests.cs docs/implementation-plan/progress.md docs/implementation-plan/memory.md
git commit -m "Document launch failure policy"
```

## Notes For Junior Developer

- Do not add launch retry behavior in this task.
- Do not change process exit restart modes.
- Keep the public API unchanged.
