# Task 07: Restart Backoff Policy

## Goal

Implement per-alias exponential restart backoff.

## Implementation Steps

1. Add an internal backoff state object.
2. First failure returns `MinBackoffSeconds` as a normalized duration.
3. Repeated failures double the previous delay.
4. Delay is capped at `MaxBackoffSeconds` as a normalized duration.
5. Backoff resets after a process remains alive for at least `StableRunDurationSeconds` as a normalized duration.
6. Keep backoff state scoped to one alias.

## Done Means

- Backoff is deterministic and unit-testable.
- No process launching code depends on wall-clock time directly.
- State can be reset when stable runtime is observed.

## Test Plan

Unit tests:

- First failure uses minimum delay.
- Second failure doubles delay.
- Repeated failures cap at maximum.
- Stable runtime resets to minimum.
- Independent instances do not share state.

Run:

```powershell
dotnet test
```

## Notes For Junior Developer

- Do not sleep in unit tests.
- Use the injectable clock or explicit timestamps.
