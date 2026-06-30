# Task 06: Scheduler Calculation

## Goal

Calculate the next local scheduled restart time deterministically from an injectable clock.

## Implementation Steps

1. Add an internal clock abstraction.
2. Add a system clock implementation using local host time.
3. Add scheduler calculation code that accepts:
   - current local time
   - one or more validated schedule records
4. For each schedule, calculate its next occurrence.
5. Return the earliest occurrence across schedules.
6. Collapse duplicate next occurrences.
7. Define v1 DST behavior:
   - use the next valid local occurrence
   - in repeated local-time intervals, execute once per configured occurrence

## Done Means

- Scheduler calculation has no dependency on real time in tests.
- Multiple schedules choose the earliest next occurrence.
- Duplicate schedules do not create duplicate restarts at the same time.

## Test Plan

Unit tests:

- Schedule later today returns today.
- Schedule earlier today rolls forward.
- Schedule for later weekday returns that weekday.
- Schedule rolls to next week when needed.
- Duplicate schedules collapse.
- Midnight transition is deterministic.
- DST behavior is documented in test names and assertions.

Run:

```powershell
dotnet test
```

## Notes For Junior Developer

- Keep calculation separate from timers.
- Do not trigger restarts in this task.
