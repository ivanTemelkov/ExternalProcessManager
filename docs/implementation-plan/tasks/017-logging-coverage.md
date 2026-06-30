# Task 17: Logging Coverage

## Goal

Add useful lifecycle logs through `ILogger` without introducing direct logging sinks.

## Implementation Steps

1. Add logs for manager start and stop.
2. Add logs for process start and exit.
3. Add logs for restart decisions.
4. Add logs for backoff delays.
5. Add logs for scheduled restart execution.
6. Add logs for configuration hot reload.
7. Add logs for validation errors.
8. Add logs for graceful shutdown and forced kill results.
9. Ensure the library does not write directly to:
   - console
   - files
   - EventLog
   - telemetry systems

## Done Means

- Important lifecycle events are visible through host logging.
- Logs include alias where applicable.
- Logs do not expose secrets from environment values.
- No direct logging sinks are used.

## Test Plan

Unit tests with fake logger:

- Manager start/stop logs are emitted.
- Process exit restart decision logs are emitted.
- Validation errors are logged.
- Forced kill result is logged.

Static review:

- Search for direct console/file/EventLog writes in library project.

Run:

```powershell
dotnet test
```

## Notes For Junior Developer

- Be careful logging arguments and environment values; arguments can contain secrets.
- Prefer structured logging placeholders.
