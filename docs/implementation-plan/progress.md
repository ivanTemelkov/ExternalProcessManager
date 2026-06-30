# Implementation Progress

Use this file to track task implementation status.

Statuses:

- `Not Started`
- `In Progress`
- `Implemented`
- `Blocked`
- `Needs Review`

| ID | Task | Status | Notes |
|---|---|---|---|
| 01 | Scaffold solution and projects | Implemented | Uses `IvTem.ExternalProcessManager.slnx` per project direction. |
| 02 | Define public contracts | Implemented | Public manager contract, diagnostics snapshots, status enum, and DI extension added. |
| 03 | Add configuration reader and raw models | Implemented | Raw reader preserves values and configuration paths. |
| 04 | Add validation and effective configuration normalization | Implemented | Produces effective configs plus invalid entries and validation errors. |
| 05 | Implement day and schedule parsing | Implemented | Parses scheduled restart hour and day selectors into internal records. |
| 06 | Implement scheduler calculation | Implemented | Deterministic local-time calculator with DST gap/repeat behavior. |
| 07 | Implement restart backoff policy | Implemented | Per-alias state object computes exponential delays from explicit runtime timestamps. |
| 08 | Implement process launcher abstraction | Implemented | Internal launcher maps effective config to `ProcessStartInfo` and returns disposable observable handles. |
| 09 | Implement Windows cleanup abstraction | Implemented | Starts managed processes in a Windows process group, sends CTRL+BREAK for graceful stop, then force-kills the process tree when needed. |
| 10 | Implement per-alias supervisor | Implemented | Serializes per-alias lifecycle operations and applies restart policy/backoff. |
| 11 | Implement manager reconciliation and hot reload | Implemented | Reconciles valid aliases, preserves last valid config for invalid changed aliases, and reports invalid entries in snapshots. |
| 12 | Implement diagnostics snapshots | Implemented | Snapshots refresh from current supervisor state, preserve stopped process diagnostics, and include next scheduled restart values. |
| 13 | Implement scheduled restart execution | Implemented | Supervisors schedule one-shot due timers and execute intentional restarts under the per-alias lifecycle lock. |
| 14 | Add hosted-service integration | Not Started | |
| 15 | Add manual lifecycle idempotency and disposal | Not Started | |
| 16 | Build integration test helper executable | Not Started | |
| 17 | Add logging coverage | Not Started | |
| 18 | Run final analyzer and platform pass | Not Started | |

## Progress Notes

Add dated notes below as implementation progresses.

```text
YYYY-MM-DD:
- Task:
- Change:
- Verification:
- Follow-up:
```

2026-06-30:
- Task: 01 - Scaffold solution and projects.
- Change: Created `IvTem.ExternalProcessManager.slnx`, `src/IvTem.ExternalProcessManager`, `tests/IvTem.ExternalProcessManager.Tests`, and `tests/IvTem.ExternalProcessManager.TestProcess`; added projects to the solution; added the test project reference to the library; enabled AOT, trim, AOT analyzer, and warning-as-error settings on the library; removed generated placeholder source files; updated docs to use `.slnx`.
- Verification: `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 02.

2026-06-30:
- Task: 01 - Scaffold solution and projects.
- Change: Renamed the scaffolded solution and projects to use the `IvTem.ExternalProcessManager` prefix: `IvTem.ExternalProcessManager.slnx`, `src/IvTem.ExternalProcessManager`, `tests/IvTem.ExternalProcessManager.Tests`, and `tests/IvTem.ExternalProcessManager.TestProcess`; updated project references and documentation.
- Verification: `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 02.

2026-06-30:
- Task: 02 - Define public contracts.
- Change: Added `IExternalProcessManager`, immutable diagnostics snapshot records, `ExternalProcessStatus`, and `AddExternalProcessManager`; registered an internal placeholder manager until lifecycle implementation tasks replace it; added compile-oriented public contract tests.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 03.
- Memory: Added decisions for immutable snapshot collections and the temporary internal manager registration.

2026-06-30:
- Task: 03 - Add configuration reader and raw models.
- Change: Added internal raw configuration records and `ExternalProcessConfigurationReader` that manually reads `Processes`, launch fields, argument arrays, environment children, restart settings, scheduled restart settings, and configuration paths from `IConfiguration`.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 04.
- Memory: Added decisions for path-preserving raw values and scalar/array day-of-week reading.

2026-06-30:
- Task: 04 - Add validation and effective configuration normalization.
- Change: Added internal effective configuration records, restart and argument-mode enums, invalid-entry records, and `ExternalProcessConfigurationValidator`; validation covers required/unique aliases, required file names, restart mode parsing, duration parsing, positive duration checks, `MinBackoff <= MaxBackoff`, defaults, environment normalization, and `ArgumentList` preference.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 05.
- Memory: Added decisions for duplicate-alias handling and carrying scheduled restart values forward unparsed until Task 05.

2026-06-30:
- Task: 05 - Implement day and schedule parsing.
- Change: Replaced raw effective scheduled restart strings with parsed `TimeOnly` and `DayOfWeek` records; validation now accepts exact `HH:mm`, `All`, single day names, comma-separated names, pipe-separated names, arrays, and duplicate day collapse while reporting invalid hour/day paths.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 06.
- Memory: Added decisions for parsed schedule representation and `All` expansion.

2026-06-30:
- Task: 06 - Implement scheduler calculation.
- Change: Added an internal local clock abstraction, registered the system local clock through `AddExternalProcessManager`, and added a deterministic scheduled restart calculator that returns sorted distinct next occurrences and the earliest next restart across validated schedules; DST gaps skip invalid local occurrences and ambiguous local times produce one configured occurrence.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 07.
- Memory: Added decisions for timezone-aware schedule calculation, DST gap handling, and ambiguous local-time handling.

2026-06-30:
- Task: 07 - Implement restart backoff policy.
- Change: Added internal `RestartBackoffState` for per-alias exponential restart delays; first failures use `MinBackoff`, repeated failures double up to `MaxBackoff`, and observed stable runtimes reset the next delay to `MinBackoff`.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 08.
- Memory: Added decision for explicit timestamp-driven backoff state reset.

2026-06-30:
- Task: 08 - Implement process launcher abstraction.
- Change: Added internal `IProcessLauncher` / `IProcessHandle` contracts, a Windows `ProcessStartInfo` mapper and launcher, disposable process handles with process ID/start time/exit observation, DI registration, and mapping plus real-exit tests.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 09; diagnostics-specific process ID integration remains for the later manager/diagnostics tasks.
- Memory: Added decisions for inspectable start-info mapping and canceling uncompleted exit observation when handles are disposed.

2026-06-30:
- Task: 09 - Implement Windows cleanup abstraction.
- Change: Added internal cleanup contracts and result models, Windows CTRL+BREAK signaling, force-kill process-tree fallback, DI registration, process-group creation in the Windows launcher, command-line/environment block helpers for native launch, and helper-process integration tests for graceful stop, forced stop, and child cleanup.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors; `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 10; supervisor code should call `IProcessCleanup.Stop` before disposing running handles.
- Memory: Added decisions for Windows process-group launch and cleanup fallback behavior.

2026-06-30:
- Task: 10 - Implement per-alias supervisor.
- Change: Added `ExternalProcessSupervisor` with serialized start/stop/exit handling, intentional-stop suppression, restart-mode decisions, restart backoff, snapshot state tracking, and injectable restart delay; registered the delay service; added fake-based lifecycle and restart-policy tests.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 11; manager reconciliation should create one supervisor per valid alias and compose supervisor snapshots with validation state.
- Memory: Added decisions for versioned restart suppression and injectable restart delay.

2026-06-30:
- Task: 11 - Implement manager reconciliation and hot reload.
- Change: Replaced the placeholder manager with a configuration-reading reconciler, reload-token subscription, case-insensitive alias diffing, supervisor factory creation, invalid-entry diagnostics, and stop/removal handling; registered configuration reader, validator, configuration source, and supervisor factory in DI; added manager hot-reload tests.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 12 to refine diagnostics snapshots, including scheduled restart fields and any additional runtime state presentation.
- Memory: Added decisions for manager-owned reload subscription, supervisor factory test seam, and invalid changed alias diagnostics overlay.

2026-06-30:
- Task: 12 - Implement diagnostics snapshots.
- Change: Refreshed manager snapshots from current supervisor state on read, retained stopped supervisor diagnostics after manager stop, kept immutable validation state for invalid aliases, restarted retained supervisors on later manual start, and populated `NextScheduledRestart` from validated schedules through the local clock.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 13; scheduled restart execution should update runtime state and reuse the diagnostics next-occurrence calculation.
- Memory: Added decisions for on-demand snapshot refresh and retaining stopped supervisors for diagnostics/manual restart.

2026-06-30:
- Task: 13 - Implement scheduled restart execution.
- Change: Added an internal scheduled-restart timer abstraction and production timer, wired one timer per supervisor, scheduled the next validated local occurrence on start/restart/stopped-supervised states, executed due restarts as intentional lifecycle operations, incremented restart diagnostics, refreshed the next due time after execution, and replaced supervisors when schedule-only hot reload changes occur.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors; `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 14; hosted-service integration should dispose manager-owned services through DI and should not add a separate configuration watcher.
- Memory: Added decisions for supervisor-owned one-shot timers and scheduled restarts starting stopped supervised processes.
