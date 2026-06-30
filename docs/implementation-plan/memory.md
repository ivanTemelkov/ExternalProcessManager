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
  - `MinBackoff`: `00:00:02`
  - `MaxBackoff`: `00:01:00`
  - `StableRunDuration`: `00:05:00`
  - `GracefulStopTimeout`: `00:00:10`
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

## Debugging Notes

Record repeatable commands, flaky test notes, and process-control observations.

```text
YYYY-MM-DD:
- Symptom:
- Investigation:
- Fix or workaround:
```
