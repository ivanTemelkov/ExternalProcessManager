# Task 05: Day And Schedule Parsing

## Goal

Parse scheduled restart definitions into validated internal schedule records.

## Implementation Steps

1. Parse `HourOfDay` with exact `HH:mm` format.
2. Reject seconds and culture-specific formats.
3. Parse `DayOfWeek`.
4. Support:
   - `All`
   - single English day name
   - comma-separated day names
   - pipe-separated day names
   - configuration arrays
5. Use culture-invariant English day names.
6. Remove duplicate days within one schedule.
7. Return validation errors for invalid hour or day values.

## Done Means

- Valid schedule entries become internal schedule records.
- Invalid schedule entries produce structured validation errors.
- All documented day formats are supported.

## Test Plan

Unit tests:

- `All`.
- `Monday`.
- `Monday,Friday`.
- `Monday|Friday`.
- array values `Monday` and `Friday`.
- duplicate days collapse.
- invalid day fails.
- invalid hour fails.
- hour with seconds fails.
- multiple schedule entries parse.

Run:

```powershell
dotnet test
```

## Notes For Junior Developer

- This task only parses schedules.
- The next-run calculation belongs to Task 06.
