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
| 14 | Add hosted-service integration | Implemented | Registers an `IHostedService` adapter that starts and stops the singleton manager with Generic Host. |
| 15 | Add manual lifecycle idempotency and disposal | Implemented | Manager start/stop are covered as idempotent, and disposal stops retained supervisors before disposing them. |
| 16 | Build integration test helper executable | Implemented | Helper supports deterministic exit, delayed exit, run-until-killed, child process, graceful CTRL+BREAK, and forced-cleanup modes. |
| 17 | Add logging coverage | Implemented | Lifecycle, validation, restart, scheduled restart, hot-reload, and cleanup outcomes are logged through `ILogger`. |
| 18 | Run final analyzer and platform pass | Implemented | Build/test/analyzer pass is clean; public surface and platform behavior reviewed. |
| 19 | Add sample host application | Implemented | Adds runnable Generic Host and worker samples under `samples/`. |

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
- Change: Added internal effective configuration records, restart and argument-mode enums, invalid-entry records, and `ExternalProcessConfigurationValidator`; validation covers required/unique aliases, required file names, restart mode parsing, duration seconds parsing, positive duration checks, `MinBackoffSeconds <= MaxBackoffSeconds`, defaults, environment normalization, and `ArgumentList` preference.
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
- Change: Added internal `RestartBackoffState` for per-alias exponential restart delays; first failures use the configured minimum backoff, repeated failures double up to the configured maximum backoff, and observed stable runtimes reset the next delay to the minimum backoff.
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

2026-06-30:
- Task: 14 - Add hosted-service integration.
- Change: Added `ExternalProcessManagerHostedService`, registered it with `TryAddEnumerable`, added hosting abstraction dependencies, and covered DI registration plus Generic Host start/stop behavior with tests.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors; `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 15; lifecycle idempotency and disposal should review repeated hosted/manual start-stop interactions after the hosted adapter.
- Memory: Added decision for singleton manager lifecycle ownership through an `IHostedService` adapter.

2026-06-30:
- Task: 15 - Add manual lifecycle idempotency and disposal.
- Change: Added async disposal to the public manager contract, made manager disposal stop retained supervisors before disposing them, and added manual lifecycle tests for repeated start, repeated stop, synchronous disposal, asynchronous disposal, and real-process cleanup through manual `DisposeAsync`.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors; `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 16 to expand the helper executable for remaining integration scenarios.
- Memory: Added decision for manager disposal owning a final stop-before-dispose pass.

2026-06-30:
- Task: 16 - Build integration test helper executable.
- Change: Expanded the helper executable with documented modes for immediate exit, delayed exit, run-until-killed, generic child spawning, CTRL+BREAK handling, CTRL+BREAK ignoring, and retained the existing child-ignore compatibility mode; added helper-specific integration tests for each required behavior.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors; `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 17 to add logging coverage.
- Memory: Added decision for preserving the existing `spawn-child-ignore-ctrl-break` mode as the forced process-tree cleanup scenario while adding the more general `spawn-child` mode.

2026-06-30:
- Task: 17 - Add logging coverage.
- Change: Added source-generated `ILogger` messages for manager start/stop, hot reload, validation errors, supervisor process start/exit/restart/backoff/scheduled restart decisions, and graceful/forced cleanup outcomes; registered logging abstractions without adding a sink; added in-memory logger tests for manager lifecycle, validation errors, restart decisions, and forced-kill cleanup logs.
- Verification: `rg "Console\.|File\.|EventLog|Telemetry|Trace\." src/IvTem.ExternalProcessManager -n` found no direct logging sinks; `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 101 passing tests; `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 18 for the final analyzer and platform pass.
- Memory: Added decision for source-generated structured logs and logging registration without providers.

2026-06-30:
- Task: 18 - Run final analyzer and platform pass.
- Change: Reviewed analyzer/trim/AOT settings, public API shape, suppression usage, direct sink usage, diagnostics surface, and Windows-only behavior; added an internal platform guard so launch and cleanup fail fast with `PlatformNotSupportedException` on unsupported OSes before Windows interop is reached.
- Verification: `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors; `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 101 passing tests; public API scan shows only `ExternalProcessManagerSnapshot`, `ExternalProcessSnapshot`, `ExternalProcessStatus`, `ExternalProcessValidationError`, `IExternalProcessManager`, and `ServiceCollectionExtensions`; suppression scan found no production suppressions; direct sink scan found no `Console`, `File`, `EventLog`, telemetry, or trace usage in the library.
- Follow-up: v1 implementation tasks are complete.
- Memory: Added decision for explicit runtime platform guards at Windows process launch and cleanup boundaries.

2026-06-30:
- Task: 19 - Add sample host application.
- Change: Added the planned task to `TASKS.md` and created `tasks/019-sample-host-application.md` with implementation steps, done criteria, and verification commands.
- Verification: Documentation-only change; no build or test run.
- Follow-up: Implement Task 19.
- Memory: Added decision for self-contained sample apps with sample-host-owned worker path configuration.

2026-06-30:
- Task: 19 - Add sample host application.
- Change: Added `IvTem.ExternalProcessManager.SampleHost` and `IvTem.ExternalProcessManager.SampleWorker` under `samples/`; the host uses `Host.CreateApplicationBuilder`, registers `AddExternalProcessManager`, loads `appsettings.json`, layers worker path and argument-list overrides in memory, logs diagnostics snapshots through a background service, exits clearly on non-Windows, and supports `SampleHost:RunSeconds` for finite smoke tests.
- Verification: `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors; `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 101 passing tests; `dotnet run --project samples/IvTem.ExternalProcessManager.SampleHost -- --SampleHost:RunSeconds=8` started the sample worker, logged `Running` diagnostics with process ID, restart count, and next scheduled restart, then stopped the worker with graceful CTRL+BREAK cleanup.
- Follow-up: None currently.
- Memory: Added decision for resolving the sample worker from the host output folder so `dotnet run --project samples/IvTem.ExternalProcessManager.SampleHost` works from the repository root.

2026-06-30:
- Task: 19 - Add sample host application.
- Change: Made both sample projects Native AOT publish-ready, copied the published sample worker beside the published host, made worker path resolution prefer side-by-side publish output while preserving `dotnet run`, and documented normal plus AOT sample commands.
- Verification: `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors; `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 101 passing tests; `dotnet run --project samples/IvTem.ExternalProcessManager.SampleHost -- --SampleHost:RunSeconds=4` started and gracefully stopped the worker; `dotnet publish samples/IvTem.ExternalProcessManager.SampleHost/IvTem.ExternalProcessManager.SampleHost.csproj -c Release -r win-x64` completed with no warnings; the published host executable started the side-by-side published worker and stopped it gracefully.
- Follow-up: None currently.
- Memory: Added decision for publish-only side-by-side worker resolution to avoid selecting copied apphost stubs from normal build output.

2026-06-30:
- Task: Cross-cutting configuration schema update.
- Change: Renamed restart timing configuration keys to `MinBackoffSeconds`, `MaxBackoffSeconds`, `StableRunDurationSeconds`, and `GracefulStopTimeoutSeconds`; validation now parses positive integer seconds and converts them to internal `TimeSpan` values; documentation, tests, and sample configuration were updated to the new schema.
- Verification: `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors; `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 103 passing tests; `dotnet run --project samples/IvTem.ExternalProcessManager.SampleHost -- --SampleHost:RunSeconds=8` started the sample worker, reported diagnostics, and stopped gracefully.
- Follow-up: None currently.
- Memory: Added decision for the breaking seconds-based restart timing schema.

2026-07-01:
- Task: Improvements 001 - Add SonarAnalyzer.CSharp build enforcement.
- Change: Added root `Directory.Build.props` with `SonarAnalyzer.CSharp` version `10.27.0.140913`, added `.editorconfig` analyzer policy to preserve project style for explicit `== false` negation and explicit loops, resolved newly enforced analyzer findings in library, tests, and samples, and split sample host helper types into namespaced files.
- Verification: `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors; `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 103 passing tests; analyzer/suppression scan found the expected package reference, narrow `.editorconfig` rule overrides, and one locally justified `S6966` pragma for the synchronous dispose test.
- Follow-up: Continue with improvement Task 002.
- Memory: Added decisions for SonarAnalyzer enforcement, preserving `== false`, preserving explicit loops, and the local sync-dispose test suppression.

2026-07-01:
- Task: Improvements 002 - Log hot-reload background failures.
- Change: Added a guarded hot-reload reconciliation wrapper that logs unexpected background reload failures through source-generated event `1011` while keeping expected shutdown/disposal exceptions quiet; added a focused manager test that triggers a reload start failure through the fake supervisor seam and verifies the error log is emitted.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 104 passing tests; `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with improvement Task 003.
- Memory: Added decision for keeping exception handling at the fire-and-forget reload boundary and filtering expected shutdown exceptions there.

2026-07-01:
- Task: Improvements 003 - Log scheduled timer callback failures.
- Change: Added source-generated error logging around production scheduled restart timer callbacks, while preserving benign handling for cancellation and disposal races; added focused timer tests for unexpected callback failures, cancellation, and disposal exceptions.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx --filter SystemScheduledRestartTimerTests` succeeded with 3 passing tests; `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 107 passing tests; `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with improvement Task 004.
- Memory: Added decision for passing the factory logger into timer instances to keep the production timer internal while satisfying Sonar logger-type enforcement.

2026-07-01:
- Task: Improvements 004 - Harden stop cancellation state.
- Change: Delayed publishing the manager as stopped until every supervisor stops successfully; canceled stops now refresh diagnostics, log warning event `1012`, and rethrow cancellation so callers can retry cleanup with a fresh token.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx --filter CanceledStopKeepsManagerRunningAndCanBeRetried` succeeded; `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 108 passing tests; `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with improvement Task 005.
- Memory: Added decision for canceled manager stops preserving `IsRunning` and retrying the full supervisor stop pass.

2026-07-01:
- Task: Improvements 005 - Align schedule-only reload policy.
- Change: Documented that v1 applies valid schedule-only hot reload changes by replacing the alias supervisor and restarting the process, with diagnostics reflecting the replacement supervisor's next scheduled restart.
- Verification: Confirmed `ExternalProcessManager.AreEquivalent` includes scheduled restart differences and `ExternalProcessManagerTests.ReloadScheduleOnlyChangeReplacesSupervisorWithNewSchedule` asserts replacement behavior; `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 108 passing tests; `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with improvement Task 006.
- Memory: Added decision for keeping the simpler v1 schedule-only reload replacement behavior.

2026-07-01:
- Task: Improvements 006 - Define launch failure policy.
- Change: Documented the v1 launch failure policy and added a supervisor test that verifies launch failures move diagnostics to `Faulted`, set `LastError`, do not increment `RestartCount`, and do not request restart backoff.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx --filter LaunchFailureTransitionsToFaultedWithoutRestartDelay` succeeded; `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 109 passing tests; `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with improvement Task 007.
- Memory: Added decision for the v1 no-retry launch failure policy.

2026-07-01:
- Task: Improvements 007 - Run final improvement verification.
- Change: Confirmed improvement Tasks 001-006 are implemented and committed; reviewed improvement progress and memory notes; ran the full build, full test suite, sample host smoke test, analyzer/suppression search, stale-status search, and direct logging sink search.
- Verification: `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors; `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 109 passing tests; `dotnet run --project samples/IvTem.ExternalProcessManager.SampleHost -- --SampleHost:RunSeconds=8` started the sample worker, reported running diagnostics, and stopped it gracefully; `rg "TODO|FIXME|Needs Review|Blocked|SuppressMessage|NoWarn" docs src tests samples` returned only status-legend and task-instruction references; `rg "Console\.|File\.|EventLog|Telemetry|Trace\." src/IvTem.ExternalProcessManager -n` returned no direct logging sinks.
- Follow-up: Improvement batch is ready for review.
- Memory: No new memory entry needed; final verification introduced no new requirements, gotchas, or implementation decisions.

2026-07-01:
- Task: Task 19 sample host application follow-up - Add failing worker backoff stress sample.
- Change: Added `IvTem.ExternalProcessManager.FailingSampleWorker`, configured the sample host to supervise it as `failing-sample-worker`, generalized worker path resolution for multiple sample workers, copied the failing worker during host publish, and documented the backoff stress behavior.
- Verification: `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors; `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 109 passing tests; `dotnet run --project samples/IvTem.ExternalProcessManager.SampleHost -- --SampleHost:RunSeconds=8` started the normal worker, repeatedly restarted the failing worker after exit code `42`, and showed backoff delays of 1, 2, 4, and 8 seconds.
- Follow-up: None currently.
- Memory: No new memory entry needed; this extends the existing sample-worker resolution and publish-copy decisions without adding new runtime requirements.
