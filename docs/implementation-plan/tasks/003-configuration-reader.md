# Task 03: Add Configuration Reader And Raw Models

## Goal

Read raw process definitions from `IConfiguration` without making reflection-based binding the only supported path.

## Implementation Steps

1. Add internal raw configuration models for:
   - process entry
   - restart settings
   - scheduled restart settings
2. Add an internal configuration reader that accepts an `IConfiguration` section.
3. Read `Processes` children manually.
4. Read process fields:
   - `Alias`
   - `FileName`
   - `Arguments`
   - `ArgumentList`
   - `WorkingDirectory`
   - `Environment`
   - `Restart`
   - `ScheduledRestarts`
5. Support `ArgumentList` as child values.
6. Support `Environment` as key-value children.
7. Preserve configuration paths for validation errors.

## Done Means

- The reader can load an empty or missing `Processes` section.
- Raw entries preserve all supported fields.
- No validation decisions beyond basic reading are made here.
- Validation path information is available for later tasks.

## Test Plan

Unit tests:

- Missing root section returns no processes.
- Empty `Processes` returns no processes.
- Minimal process reads alias and file name.
- `Arguments` only is read.
- `ArgumentList` only is read.
- Both `Arguments` and `ArgumentList` are read.
- `Environment` dictionary is read.
- `WorkingDirectory` is read.
- Restart and scheduled restart child sections are read.

Run:

```powershell
dotnet test
```

## Notes For Junior Developer

- Do not parse restart modes, duration seconds, days, or schedule times in this task.
- Do not start any processes in this task.
