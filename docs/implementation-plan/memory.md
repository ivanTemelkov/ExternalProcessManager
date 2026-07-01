# Development Memory

Use this file to record common gotchas, decisions, and debugging notes discovered during implementation.

## Known Requirements To Keep In Mind

- v1 is Windows-only.
- Target framework is `net10.0`.
- The library must be suitable for Native AOT and trimming.
- Do not rely on reflection-heavy configuration binding as the only configuration path.
- `ArgumentList` wins over raw `Arguments` when both are configured.
- Alias comparison is case-insensitive.
- Default restart mode is `NonZeroExitCode`.
- Default backoff values:
  - `MinBackoffSeconds`: `2`
  - `MaxBackoffSeconds`: `60`
  - `StableRunDurationSeconds`: `300`
  - `GracefulStopTimeoutSeconds`: `10`
- Hot reload is best effort; one invalid entry must not block valid entries.
- Invalid changed configuration keeps the last valid running configuration.
- Diagnostics are snapshots, not public events.
- Scheduled restart times use local host time.

## Gotchas

Add discoveries here as they happen.

```text
YYYY-MM-DD:
- Area:
- Gotcha:
- Resolution:
```

## Decisions

Record implementation decisions that are not obvious from the docs.

```text
YYYY-MM-DD:
- Decision:
- Reason:
- Alternatives considered:
```

2026-06-30:
- Decision: Use `IvTem.ExternalProcessManager.slnx` as the canonical solution file instead of `IvTem.ExternalProcessManager.sln`.
- Reason: .NET 10 generated `.slnx` by default and the project direction is to keep the `.slnx` format.
- Alternatives considered: Creating a classic `.sln`; rejected by project direction.

2026-06-30:
- Decision: Use `IvTem.ExternalProcessManager` as the solution, library project, root namespace, and package identity.
- Reason: The library identity should carry the `IvTem` prefix while preserving the existing manager public API naming plan.
- Alternatives considered: Renaming only the library project; rejected because the solution and test projects should remain aligned with the prefixed package identity.

2026-06-30:
- Decision: Public diagnostics snapshots expose collection data as `ImmutableArray<T>`.
- Reason: Task 02 requires snapshots to be immutable after construction, and later runtime code can atomically replace complete snapshot values without defensive copying at read time.
- Alternatives considered: `IReadOnlyList<T>`; rejected for public snapshot storage because callers could still pass and mutate a backing `List<T>`.

2026-06-30:
- Decision: `AddExternalProcessManager` currently registers an internal placeholder `IExternalProcessManager` implementation that only tracks start/stop state and returns empty diagnostics.
- Reason: Task 02 needs a usable public registration contract before configuration, reconciliation, and process supervision exist.
- Alternatives considered: Leaving the service unregistered; rejected because the extension would compile but not satisfy the host integration contract.

2026-06-30:
- Decision: Raw configuration stores scalar values as `RawConfigurationValue` with both `Path` and `Value`.
- Reason: Task 04 validation needs to report precise configuration paths while Task 03 must avoid parsing or validation decisions.
- Alternatives considered: Storing only strings and recomputing paths during validation; rejected because nested fields and collection entries would be easier to misreport later.

2026-06-30:
- Decision: Raw scheduled restart configuration keeps `DayOfWeek` as both a scalar value and optional child-value collection.
- Reason: Configuration supports both string selectors and JSON-array day selectors, and preserving both shapes avoids treating an array as invalid during raw reading.
- Alternatives considered: Reading only `DayOfWeek.Value`; rejected because array-shaped configuration would lose the configured day values before schedule parsing.

2026-06-30:
- Decision: Duplicate aliases invalidate every colliding entry instead of keeping the first configured process.
- Reason: Alias is the public identity and runtime reconciliation key, so accepting one duplicate would make ordering determine which process wins.
- Alternatives considered: Accepting the first valid alias and rejecting later duplicates; rejected because hot reload behavior would be harder to reason about.

2026-06-30:
- Decision: Task 04 effective scheduled restart records carry raw schedule strings forward without parsing hour or day values.
- Reason: Task 05 explicitly owns schedule parsing and validation, while Task 04 still needs an internal normalized process model that preserves configured schedules for the next step.
- Alternatives considered: Parsing schedules during Task 04; rejected to keep implementation tasks aligned with the documented plan.

2026-06-30:
- Decision: Effective scheduled restart records store `HourOfDay` as `TimeOnly` and days as `ImmutableArray<DayOfWeek>`, with `All` expanded to the seven `DayOfWeek` values.
- Reason: Task 06 scheduler calculation should consume validated values without reparsing configuration text.
- Alternatives considered: Keeping the original strings plus parsed companion values; rejected because it would allow later runtime code to accidentally use unvalidated schedule text.

2026-06-30:
- Decision: Day selectors from configuration arrays are tokenized with the same comma and pipe separator support as scalar selectors, and duplicates are collapsed while preserving first-seen order.
- Reason: The reader preserves both scalar and array shapes, and using one parser keeps schedule behavior consistent across providers.
- Alternatives considered: Treating each array item as exactly one day; rejected because it would make arrays less tolerant than equivalent scalar configuration.

2026-06-30:
- Decision: Scheduled restart calculation is timezone-aware internally: callers pass a current local `DateTimeOffset`, and `ScheduledRestartCalculator` resolves occurrences against its configured `TimeZoneInfo`.
- Reason: This keeps tests deterministic while preserving local host time behavior for the system clock implementation.
- Alternatives considered: Using `TimeZoneInfo.Local` directly inside the calculator; rejected because DST behavior would be host-machine dependent in tests.

2026-06-30:
- Decision: Register `ILocalClock` as `SystemLocalClock` with `TryAddSingleton` in `AddExternalProcessManager`.
- Reason: Task 06 calls for an injectable clock, and `TryAddSingleton` leaves room for tests or future host integration to replace the clock before registration.
- Alternatives considered: Leaving the clock unregistered until lifecycle tasks; rejected because the abstraction would exist but not be injectable through the library's normal setup path.

2026-06-30:
- Decision: Invalid local times during DST gaps are skipped, so the calculator returns the next configured local occurrence that exists in the timezone.
- Reason: Task 06 defines v1 behavior as the next valid local occurrence.
- Alternatives considered: Moving a missing time forward to the first valid clock time after the gap; rejected because that would restart at a time that was not configured.

2026-06-30:
- Decision: Ambiguous local times during DST fall-back resolve to a single occurrence, choosing the earliest future instant for that configured local time.
- Reason: v1 should execute once per configured occurrence while still allowing a manager that starts mid-repeat to find the next future instant.
- Alternatives considered: Returning both offsets for the repeated local time; rejected because duplicate execution is explicitly out of scope for v1.

2026-06-30:
- Decision: `RestartBackoffState` does not read wall-clock time; callers pass process start, failure, or observation timestamps explicitly.
- Reason: Task 07 requires deterministic unit tests and future supervisors already have process start and exit observations available.
- Alternatives considered: Injecting `ILocalClock` directly into the backoff state; rejected because the state only needs elapsed runtime observations and should remain independent of scheduling/local-time concerns.

2026-06-30:
- Decision: Keep `ProcessStartInfo` mapping in an internal `WindowsProcessStartInfoFactory` used by `WindowsProcessLauncher`.
- Reason: Task 08 mapping rules can be tested without starting a process, while actual `Process` construction stays isolated inside the launcher implementation.
- Alternatives considered: Testing only through launched processes; rejected because argument/environment/working-directory mapping would be slower and more fragile to inspect.

2026-06-30:
- Decision: Disposing a process handle cancels its exit-observation task only if an exit result has not already been recorded.
- Reason: Awaiters should not hang forever when a handle is disposed before process exit, and completed exit observations should remain stable.
- Alternatives considered: Leaving the task incomplete on dispose; rejected because future supervisors may dispose handles during lifecycle transitions.

2026-06-30:
- Decision: Managed processes are started through a minimal Windows `CreateProcessW` wrapper with `CREATE_NEW_PROCESS_GROUP`.
- Reason: Targeted CTRL+BREAK requires a process group ID, and `ProcessStartInfo` does not expose the creation flag needed to make the child process ID its group ID.
- Alternatives considered: Using `Process.Start` and attaching to the child console; rejected because it cannot reliably target only the managed process group.

2026-06-30:
- Decision: Windows cleanup first attempts CTRL+BREAK for the managed process group, waits the configured graceful timeout, then uses `Process.Kill(entireProcessTree: true)` as the force-kill fallback.
- Reason: The graceful path supports console helpers that cooperate, while the fallback reliably cleans up descendants for uncooperative or non-console processes.
- Alternatives considered: Root-process-only kill; rejected because cleanup scope requires descendants to be terminated too.

2026-06-30:
- Decision: `ExternalProcessSupervisor` uses a per-alias serialized operation lock plus a monotonically increasing version to suppress stale exit observers and pending restarts after explicit starts or stops.
- Reason: Exit observation and restart delay happen asynchronously, and version checks prevent old lifecycle work from starting a duplicate process after a newer lifecycle command wins.
- Alternatives considered: Holding the operation lock for the entire restart backoff delay; rejected because explicit stop/start commands should be able to supersede a pending restart.

2026-06-30:
- Decision: Restart backoff waiting is behind an internal `IRestartDelay` abstraction registered as `SystemRestartDelay`.
- Reason: The supervisor needs deterministic tests that prove a backoff delay is requested before relaunch without making the test suite sleep for real backoff durations.
- Alternatives considered: Calling `Task.Delay` directly in the supervisor; rejected because restart tests would either be slow or unable to assert pending-restart behavior cleanly.

2026-06-30:
- Decision: `ExternalProcessManager` owns a reload-token subscription from the supplied configuration section and reconciles changes directly from the existing raw reader and validator.
- Reason: v1 must use host-provided configuration reload and avoid a separate file watcher while keeping invalid entries best-effort and non-throwing.
- Alternatives considered: Adding options binding or a file watcher; rejected because the current configuration reader is path-preserving, trim-friendly, and already handles the raw schema.

2026-06-30:
- Decision: Add an internal `IExternalProcessSupervisorFactory` and `IExternalProcessSupervisor` seam while keeping `ExternalProcessSupervisor` as the production implementation.
- Reason: Manager reconciliation needs focused unit tests for alias diff behavior without launching Windows processes.
- Alternatives considered: Testing manager reconciliation through the real launcher; rejected because it would make hot-reload tests slower, platform-side-effect-heavy, and less precise.

2026-06-30:
- Decision: When a hot-reloaded alias becomes invalid but matches an existing supervisor, the manager keeps the existing supervisor and overlays the current validation errors onto that alias snapshot without changing the runtime status.
- Reason: Diagnostics should show that the last valid process is still running while also surfacing the current invalid configuration.
- Alternatives considered: Marking the process snapshot as `InvalidConfiguration`; rejected because that would hide the actual running/stopped runtime state for the preserved supervisor.

2026-06-30:
- Decision: Manager `GetSnapshot()` refreshes the published immutable snapshot from current supervisor snapshots under a short lock before returning it.
- Reason: Supervisor status can change outside configuration reconciliation, such as observed exits and restart-pending transitions, and diagnostics must reflect that current runtime state without waiting for logs or a reload.
- Alternatives considered: Updating the manager snapshot only at reconciliation boundaries; rejected because process exit and restart state would be stale.

2026-06-30:
- Decision: `StopAsync` stops supervisors but keeps them undisposed and retained in the manager until removal, replacement, or manager disposal.
- Reason: Host diagnostics after stop should still show the last managed aliases as stopped, and a later manual `StartAsync` can restart unchanged retained supervisors without rebuilding process identity state.
- Alternatives considered: Disposing and clearing supervisors during `StopAsync`; rejected because post-stop snapshots lost all process rows and manual restart needed to reconstruct unchanged aliases.

2026-06-30:
- Decision: Supervisor diagnostics calculate `NextScheduledRestart` from the validated schedule list and injected local clock when snapshots are read or updated.
- Reason: Task 12 needs schedule information in diagnostics before Task 13 adds execution timers, and the existing calculator already defines local-time and DST behavior.
- Alternatives considered: Leaving `NextScheduledRestart` null until scheduled execution exists; rejected because host UI diagnostics would not show the effective schedule.

2026-06-30:
- Decision: Scheduled restart execution uses one supervisor-owned one-shot timer created by an internal timer factory.
- Reason: The supervisor already serializes per-alias lifecycle work, so due restarts can reuse the existing operation lock, intentional-stop versioning, cleanup, and launch flow without a manager-level scheduler racing alias operations.
- Alternatives considered: A manager-level scheduler for all aliases; rejected because it would need to coordinate with each supervisor's lifecycle lock and duplicate per-alias state already owned by the supervisor.

2026-06-30:
- Decision: A due scheduled restart starts a stopped supervised process and increments `RestartCount` even when no running handle is found.
- Reason: The scheduled restart feature represents an executed maintenance cycle, and the docs require stopped-but-supervised aliases to be started at due time while diagnostics reflect that execution.
- Alternatives considered: Leaving stopped aliases stopped until manual start or crash policy restart; rejected because scheduled maintenance would not recover a supervised process that exited normally before the due time.

2026-06-30:
- Decision: Hosted integration uses a small `IHostedService` adapter over the singleton `IExternalProcessManager`, registered with `TryAddEnumerable`.
- Reason: Generic Host can own lifecycle without changing the manual manager API, and repeated calls to `AddExternalProcessManager` do not add duplicate hosted-service adapters or managers.
- Alternatives considered: Making `ExternalProcessManager` implement `IHostedService` directly; rejected because it would mix public manual lifecycle concerns with host lifecycle plumbing.

2026-06-30:
- Decision: `IExternalProcessManager` extends `IAsyncDisposable`, while the internal manager also keeps synchronous `IDisposable`; both disposal paths stop retained supervisors before disposing them.
- Reason: Manual users and DI containers need a deterministic cleanup path that does not rely on the hosted-service adapter being called first, and disposal must not leave managed child processes running.
- Alternatives considered: Keeping only synchronous `IDisposable`; rejected because graceful process cleanup is asynchronous. Keeping disposal as handle-only cleanup; rejected because it bypassed the supervised stop path.

2026-06-30:
- Decision: Keep `spawn-child-ignore-ctrl-break` as a compatibility helper mode where both parent and child ignore CTRL+BREAK, and add `spawn-child` as the generic child-process helper mode.
- Reason: Existing cleanup tests depend on `spawn-child-ignore-ctrl-break` requiring the force-kill fallback for the whole process tree, while Task 16 also needs a straightforward child-spawn mode for helper-specific integration coverage.
- Alternatives considered: Replacing the old mode with generic `spawn-child`; rejected because it changed the cleanup outcome from force-kill to graceful parent exit.

2026-06-30:
- Decision: Use source-generated `LoggerMessage` methods for lifecycle logging and call `AddLogging()` during service registration without adding any provider.
- Reason: The library needs structured host-visible logs while staying sink-free, warning-clean, and friendly to trimming/AOT.
- Alternatives considered: Calling `ILogger` extension methods directly; rejected because the project treats analyzer cleanliness as part of the build contract. Registering a concrete logger provider; rejected because hosts own logging sinks.

2026-06-30:
- Decision: Add an internal `WindowsPlatform.ThrowIfUnsupported()` guard at process launch and cleanup boundaries.
- Reason: v1 is Windows-only, and unsupported OS behavior should fail fast with a clear `PlatformNotSupportedException` before reaching Windows P/Invoke or process-control APIs.
- Alternatives considered: Relying on documented Windows-only support and eventual interop failures; rejected because Task 18 requires explicit platform behavior. Guarding service registration; rejected because hosts can still construct diagnostics-only service graphs on non-Windows and only process-control operations require Windows.

2026-06-30:
- Decision: Task 19 sample applications should be self-contained under `samples/`, with a Generic Host sample supervising a sample worker and the sample host layering the worker path into configuration at startup.
- Reason: The sample should demonstrate the real public host integration API while remaining runnable from the repository without hard-coded machine paths or dependency on test-only executables.
- Alternatives considered: Reusing `IvTem.ExternalProcessManager.TestProcess`; rejected because it is test infrastructure rather than a public usage sample. Shipping only placeholder configuration; rejected because the sample would not be runnable out of the box.

2026-06-30:
- Decision: The sample host sets its content root to `AppContext.BaseDirectory`, copies `appsettings.json` to output, and resolves the sample worker from the sibling worker project's output using the host build configuration.
- Reason: `dotnet run --project samples/IvTem.ExternalProcessManager.SampleHost` can be launched from the repository root, so relying on the current directory would make sample configuration and worker discovery brittle.
- Alternatives considered: Hard-coding source-tree paths; rejected because it would not survive different repository locations. Copying the worker into the host output; rejected because a sibling project reference and deterministic output lookup keep the sample simpler while still building the worker automatically.

2026-06-30:
- Decision: The sample host resolves a side-by-side worker executable only from publish output, then falls back to the worker project's build output for normal `dotnet run`.
- Reason: Native AOT publish places the worker executable beside the host, but normal build output can contain copied apphost stubs without the matching worker DLL; using those stubs causes immediate worker exits.
- Alternatives considered: Always preferring side-by-side worker executables; rejected because normal `dotnet run` can select an incomplete copied apphost. Publishing only the host and expecting users to publish the worker manually; rejected because the AOT sample should be runnable from the host publish folder.

2026-06-30:
- Decision: Restart timing configuration uses integer seconds keys: `MinBackoffSeconds`, `MaxBackoffSeconds`, `StableRunDurationSeconds`, and `GracefulStopTimeoutSeconds`.
- Reason: Configuration should avoid host-facing `TimeSpan` string parsing and make the unit explicit in the option names while preserving internal `TimeSpan` use for lifecycle code.
- Alternatives considered: Accepting the old `TimeSpan` keys as aliases; rejected because v1 can take a breaking schema cleanup before compatibility commitments.

2026-07-01:
- Decision: Enforce `SonarAnalyzer.CSharp` locally through root `Directory.Build.props` using version `10.27.0.140913`.
- Reason: The improvement plan requires SonarAnalyzer to participate in local builds for the library, tests, and samples.
- Alternatives considered: Adding the package to individual project files; rejected because a root props file keeps analyzer enforcement consistent and easier to maintain.

2026-07-01:
- Decision: Disable Sonar rule `S1125` in `.editorconfig` and keep the project convention of explicit boolean negation with `== false`.
- Reason: The coding style intentionally prefers visible negation, and project style takes priority over the default Sonar style rule.
- Alternatives considered: Rewriting negation to `!`; rejected because it conflicts with the documented project style.

2026-07-01:
- Decision: Disable Sonar rule `S3267` in `.editorconfig` and preserve explicit loops when they match surrounding code better than LINQ.
- Reason: The rule is stylistic, and the coding style requires matching existing surrounding style over analyzer-preferred expression changes.
- Alternatives considered: Rewriting loops to LINQ; rejected for spots where explicit lifecycle or collection-building loops read more clearly in the existing code.

2026-07-01:
- Decision: Keep a local `S6966` pragma in the manager synchronous disposal test.
- Reason: The test intentionally calls `Dispose()` to verify the synchronous disposal path, so replacing it with `DisposeAsync()` would stop testing the behavior under test.
- Alternatives considered: Converting the test to `DisposeAsync`; rejected because a separate async-dispose test already covers that path.

2026-07-01:
- Decision: Handle unexpected hot-reload exceptions at the fire-and-forget callback boundary with a source-generated error log, while filtering expected shutdown `OperationCanceledException` and disposal `ObjectDisposedException` cases there.
- Reason: Reload reconciliation should remain best effort and observable without spreading catch-all handling through normal configuration application code.
- Alternatives considered: Catching exceptions inside `ApplyConfiguration`; rejected because it would hide which fire-and-forget path failed and could interfere with normal start/stop error behavior.

2026-07-01:
- Decision: Scheduled restart timers accept the factory-owned `ILogger` and log unexpected callback failures from the timer's fire-and-forget boundary.
- Reason: Timer instances are created internally by the factory, and Sonar rule `S6672` requires constructor-injected loggers to use the enclosing factory type while the timer still needs an observable callback failure boundary.
- Alternatives considered: Injecting `ILogger<SystemScheduledRestartTimer>` into the factory; rejected because it violates the enforced Sonar logger-type rule. Exposing timer events publicly; rejected because diagnostics remain snapshot-based and logs are the intended historical signal.

2026-07-01:
- Decision: A canceled manager stop leaves `IsRunning` true, refreshes diagnostics, logs warning event `1012`, and retries the full supervisor stop pass on the next `StopAsync` call.
- Reason: A partial stop can leave later aliases running, so manager diagnostics must not claim a completed shutdown while cleanup is incomplete.
- Alternatives considered: Marking the manager stopped before cleanup or after the first canceled supervisor; rejected because both states can hide still-running supervised processes from host health views.

2026-07-01:
- Decision: Keep v1 schedule-only hot reload behavior as supervisor replacement and process restart, rather than in-place timer rescheduling.
- Reason: The existing equivalence comparison already treats schedule changes as effective configuration changes, replacement reuses the established lifecycle path, and diagnostics naturally come from the replacement supervisor's validated schedule state.
- Alternatives considered: Non-disruptive in-place schedule rescheduling; deferred because it would add a second scheduler update path and more lifecycle concurrency surface for a v1 follow-up.

## Debugging Notes

Record repeatable commands, flaky test notes, and process-control observations.

```text
YYYY-MM-DD:
- Symptom:
- Investigation:
- Fix or workaround:
```
