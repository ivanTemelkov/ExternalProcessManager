# IvTem.ExternalProcessManager Implementation Tasks

This folder contains the implementation plan for IvTem.ExternalProcessManager v1.

- `TASKS.md`: high-level task outline.
- `progress.md`: tracker for which tasks are implemented.
- `memory.md`: shared development notes and common gotchas.
- `tasks/`: one detailed file per task.

## Task Outline

| ID | Task | Detail File |
|---|---|---|
| 01 | Scaffold solution and projects | [001-scaffold-solution.md](tasks/001-scaffold-solution.md) |
| 02 | Define public contracts | [002-public-contracts.md](tasks/002-public-contracts.md) |
| 03 | Add configuration reader and raw models | [003-configuration-reader.md](tasks/003-configuration-reader.md) |
| 04 | Add validation and effective configuration normalization | [004-validation-normalization.md](tasks/004-validation-normalization.md) |
| 05 | Implement day and schedule parsing | [005-schedule-parsing.md](tasks/005-schedule-parsing.md) |
| 06 | Implement scheduler calculation | [006-scheduler-calculation.md](tasks/006-scheduler-calculation.md) |
| 07 | Implement restart backoff policy | [007-backoff-policy.md](tasks/007-backoff-policy.md) |
| 08 | Implement process launcher abstraction | [008-process-launcher.md](tasks/008-process-launcher.md) |
| 09 | Implement Windows cleanup abstraction | [009-windows-cleanup.md](tasks/009-windows-cleanup.md) |
| 10 | Implement per-alias supervisor | [010-alias-supervisor.md](tasks/010-alias-supervisor.md) |
| 11 | Implement manager reconciliation and hot reload | [011-manager-reconciliation.md](tasks/011-manager-reconciliation.md) |
| 12 | Implement diagnostics snapshots | [012-diagnostics-snapshots.md](tasks/012-diagnostics-snapshots.md) |
| 13 | Implement scheduled restart execution | [013-scheduled-restart-execution.md](tasks/013-scheduled-restart-execution.md) |
| 14 | Add hosted-service integration | [014-hosted-service-integration.md](tasks/014-hosted-service-integration.md) |
| 15 | Add manual lifecycle idempotency and disposal | [015-manual-lifecycle-idempotency.md](tasks/015-manual-lifecycle-idempotency.md) |
| 16 | Build integration test helper executable | [016-test-helper-executable.md](tasks/016-test-helper-executable.md) |
| 17 | Add logging coverage | [017-logging-coverage.md](tasks/017-logging-coverage.md) |
| 18 | Run final analyzer and platform pass | [018-final-analyzer-platform-pass.md](tasks/018-final-analyzer-platform-pass.md) |
| 19 | Add sample host application | [019-sample-host-application.md](tasks/019-sample-host-application.md) |

## Implementation Order

Implement tasks in order unless a task explicitly says it can be done independently. The recommended milestones are:

1. Tasks 01-04: project shape, public surface, configuration, validation.
2. Tasks 05-07: deterministic scheduling and restart policy logic.
3. Tasks 08-10: process launch, cleanup, and per-process supervision.
4. Tasks 11-13: manager orchestration, hot reload, diagnostics, scheduled restart execution.
5. Tasks 14-18: integration, lifecycle hardening, helper executable, logging, and final verification.
6. Task 19: post-v1 runnable sample applications.

## Definition of v1 Complete

IvTem.ExternalProcessManager v1 is complete when:

- All tasks in `progress.md` are marked `Implemented`.
- `dotnet build` passes.
- `dotnet test` passes.
- AOT and trim analyzer warnings are resolved or narrowly justified.
- The public API stays compact and matches the docs.
- Invalid configuration is reported through diagnostics and does not crash the host.
