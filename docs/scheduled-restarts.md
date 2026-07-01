# Scheduled Restarts

## Purpose

Scheduled restarts allow a host to recycle long-running executables at predictable maintenance times.

Each process can define zero or more scheduled restart entries.

## Schedule Model

```json
"ScheduledRestarts": [
  { "HourOfDay": "23:45", "DayOfWeek": "All" },
  { "HourOfDay": "04:00", "DayOfWeek": "Monday|Friday" }
]
```

`HourOfDay`:

- required
- format is `HH:mm`
- interpreted in local host time
- seconds are not supported in v1

`DayOfWeek`:

- required
- accepts `All`
- accepts culture-invariant English day names
- accepts comma-separated names
- accepts pipe-separated names
- accepts JSON arrays if the configuration provider supplies them

## Local-Time Semantics

Schedules use the local time zone of the host machine. v1 does not support per-process time zones or UTC schedules.

The scheduler should calculate the next occurrence from an injectable clock to keep tests deterministic.

## Multiple Schedules

When multiple schedules are configured for one process:

- calculate the next occurrence for each schedule.
- execute the earliest due restart.
- after execution, calculate the next occurrence again.

If two schedules resolve to the same time, only one restart should occur.

## Scheduled Restart Execution

At the scheduled time:

1. Mark the restart as intentional.
2. Gracefully stop the process using the process lifecycle shutdown flow.
3. Kill the process tree if it does not stop before timeout.
4. Start the process again using the current valid effective configuration.
5. Update diagnostics with restart count and next scheduled restart.

If the process is already stopped:

- start it if restart mode and current manager state allow supervision.
- log that the scheduled restart found no running process.

## Hot Reload Interaction

When schedules change through hot reload:

- changed process configuration causes the process to restart.
- changed schedule-only configuration also restarts the alias in v1.
- v1 applies a valid schedule-only change by replacing the alias supervisor, then starting the replacement with the new effective schedule.
- diagnostics then reflect the replacement supervisor's new next scheduled restart.

Invalid schedule changes follow the best-effort hot reload rules:

- invalid new entries are not applied.
- invalid changed entries keep the last valid schedule for already-running aliases.
- validation errors appear in diagnostics.

## Edge Cases

The implementation should define deterministic behavior for:

- a schedule time that is already earlier today.
- day-of-week transitions at midnight.
- duplicate schedules.
- daylight saving time gaps and repeated local times.

For v1, use the next valid local occurrence. In repeated local-time intervals, execute once per configured occurrence.
