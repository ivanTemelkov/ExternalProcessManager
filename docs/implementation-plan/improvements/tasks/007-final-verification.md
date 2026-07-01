# Task 007: Run Final Improvement Verification

## Goal

Verify the full post-v1 improvement batch is clean, documented, and ready for
review.

## Required Reading

- `docs/implementation-plan/improvements/TASKS.md`
- `docs/implementation-plan/progress.md`
- `docs/implementation-plan/memory.md`
- `docs/testing.md`
- `docs/aot-readiness.md`

## Implementation Steps

1. Confirm tasks 001-006 are implemented and committed.
2. Run full build.
3. Run full tests.
4. Run the sample host smoke test on Windows.
5. Search for direct logging sinks, broad suppressions, TODOs, and stale task
   statuses.
6. Confirm `progress.md` has a dated note for each improvement task.
7. Confirm `memory.md` has each important decision or a note that no new memory
   entry was needed.
8. Add a final dated progress note for this verification task.

## Done Means

- Build and tests pass.
- Sample host smoke test passes on Windows.
- No broad analyzer suppression was introduced.
- Improvement task documentation, progress, and memory are up to date.
- The repository is ready for review.

## Validation

Run:

```powershell
dotnet build IvTem.ExternalProcessManager.slnx
dotnet test IvTem.ExternalProcessManager.slnx
dotnet run --project samples/IvTem.ExternalProcessManager.SampleHost -- --SampleHost:RunSeconds=8
rg "TODO|FIXME|Needs Review|Blocked|SuppressMessage|NoWarn" docs src tests samples
rg "Console\\.|File\\.|EventLog|Telemetry|Trace\\." src/IvTem.ExternalProcessManager -n
```

Verify:

- Build has 0 warnings and 0 errors.
- Tests pass.
- Sample worker starts and stops cleanly.
- Search results are either empty or explicitly justified.

## Stage And Commit

Run:

```powershell
git status --short
git add docs/implementation-plan/progress.md docs/implementation-plan/memory.md
git commit -m "Verify post-v1 improvements"
```

## Notes For Junior Developer

- Do not mark this task complete if any previous improvement task is incomplete.
- Do not hide warnings with suppressions during final verification.
- If validation fails, fix the failing task first and then rerun this task.
