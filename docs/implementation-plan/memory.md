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

## Debugging Notes

Record repeatable commands, flaky test notes, and process-control observations.

```text
YYYY-MM-DD:
- Symptom:
- Investigation:
- Fix or workaround:
```
