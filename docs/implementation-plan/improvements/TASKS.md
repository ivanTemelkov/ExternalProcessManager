# ExternalProcessManager Post-v1 Improvement Tasks

This folder tracks hardening improvements discovered after the v1 implementation pass.
The original v1 task list remains complete; these tasks are follow-up work.

- `TASKS.md`: high-level improvement task outline.
- `tasks/`: one detailed file per improvement task.

## Task Outline

| ID | Task | Detail File |
|---|---|---|
| 001 | Add SonarAnalyzer.CSharp build enforcement | [001-sonar-analyzer.md](tasks/001-sonar-analyzer.md) |
| 002 | Log hot-reload background failures | [002-reload-failure-logging.md](tasks/002-reload-failure-logging.md) |
| 003 | Log scheduled timer callback failures | [003-timer-failure-logging.md](tasks/003-timer-failure-logging.md) |
| 004 | Harden stop cancellation state | [004-stop-cancellation-state.md](tasks/004-stop-cancellation-state.md) |
| 005 | Align schedule-only reload policy | [005-schedule-reload-policy.md](tasks/005-schedule-reload-policy.md) |
| 006 | Define launch failure policy | [006-launch-failure-policy.md](tasks/006-launch-failure-policy.md) |
| 007 | Run final improvement verification | [007-final-verification.md](tasks/007-final-verification.md) |

## Implementation Order

Implement tasks in order. The recommended milestones are:

1. Task 001: enforce the analyzer baseline before changing runtime behavior.
2. Tasks 002-003: make background failures observable.
3. Task 004: harden lifecycle cancellation behavior.
4. Tasks 005-006: remove remaining product-policy ambiguity.
5. Task 007: verify the full improvement batch.

## Per-task Workflow

For each task:

1. Read the task file completely before editing.
2. Read any referenced domain docs before changing code or docs.
3. Keep the change scoped to the task.
4. Run the validation commands listed in the task.
5. Update `../progress.md` with the dated change summary, verification, and follow-up.
6. Update `../memory.md` with decisions, gotchas, or "No new memory entry needed."
7. Stage only files changed for the task.
8. Commit using the task's suggested commit message unless a more precise message is needed.

## Definition of Improvement Batch Complete

The improvement batch is complete when:

- All tasks in this folder are implemented.
- `dotnet build IvTem.ExternalProcessManager.slnx` passes.
- `dotnet test IvTem.ExternalProcessManager.slnx` passes.
- SonarAnalyzer.CSharp is actually enforced by the local build or explicitly documented as CI-provided.
- Background reload and scheduled timer failures are logged.
- Stop cancellation behavior is deterministic and tested.
- Schedule reload and launch failure policies are documented.
