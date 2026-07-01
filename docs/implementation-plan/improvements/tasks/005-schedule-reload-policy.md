# Task 005: Align Schedule-only Reload Policy

## Goal

Remove ambiguity between the scheduled restart docs and the current
implementation behavior for schedule-only hot reload changes.

## Required Reading

- `docs/scheduled-restarts.md`
- `docs/configuration.md`
- `docs/process-lifecycle.md`
- `src/IvTem.ExternalProcessManager/ExternalProcessManager.cs`
- `tests/IvTem.ExternalProcessManager.Tests/ExternalProcessManagerTests.cs`

## Implementation Steps

1. Keep the current v1 behavior: any valid schedule-only change replaces the
   supervisor and restarts the process.
2. Update `docs/scheduled-restarts.md` to explicitly say schedule-only hot
   reload changes restart the alias in v1.
3. Mention that diagnostics then reflect the new next scheduled restart from
   the replacement supervisor.
4. Confirm the existing test
   `ReloadScheduleOnlyChangeReplacesSupervisorWithNewSchedule` still matches
   the documented behavior.
5. Add a dated progress note to `../progress.md`.
6. Add a memory entry explaining why v1 keeps the simpler replacement behavior.

## Done Means

- Documentation and implementation agree.
- No runtime behavior changes are made in this task.
- The schedule-only reload policy is clear to host developers.

## Validation

Run:

```powershell
dotnet test IvTem.ExternalProcessManager.slnx
dotnet build IvTem.ExternalProcessManager.slnx
```

Review:

- `docs/scheduled-restarts.md`
- `ExternalProcessManagerTests.ReloadScheduleOnlyChangeReplacesSupervisorWithNewSchedule`

Verify:

- The docs clearly state that schedule-only changes restart the process.
- Existing hot-reload tests pass.

## Stage And Commit

Run:

```powershell
git status --short
git add docs/scheduled-restarts.md docs/implementation-plan/progress.md docs/implementation-plan/memory.md
git commit -m "Document schedule reload restart policy"
```

## Notes For Junior Developer

- Do not implement non-disruptive rescheduling in this task.
- Do not change the manager equivalence comparison.
- Keep the wording scoped to v1 behavior.
