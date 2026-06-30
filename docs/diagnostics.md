# Diagnostics

## Purpose

Diagnostics provide host applications with enough information to display current configuration and runtime state. Diagnostics are snapshot-based in v1. The library does not expose public event callbacks.

## Public Snapshot

The primary API:

```csharp
ExternalProcessManagerSnapshot snapshot = manager.GetSnapshot();
```

The snapshot should be immutable and safe to read without holding internal locks after it is returned.

## Manager Snapshot Fields

Suggested manager-level fields:

- `IsRunning`
- `GeneratedAt`
- `Processes`
- `ValidationErrors`

## Process Snapshot Fields

Suggested per-process fields:

- `Alias`
- `Status`
- `FileName`
- `Arguments`
- `WorkingDirectory`
- `ProcessId`
- `StartedAt`
- `ExitedAt`
- `LastExitCode`
- `RestartCount`
- `NextScheduledRestart`
- `LastError`
- `ValidationErrors`

The snapshot should include invalid configured aliases when possible, even when no process was started.

## Effective Configuration Display

Diagnostics should show the effective configuration, not every raw configuration input. For example:

- if `ArgumentList` was used, show the structured arguments.
- if defaults were applied, show resolved restart defaults.
- if a changed hot-reload entry is invalid, show the last valid effective config plus the current validation error.

## Validation Errors

Validation errors should be structured enough for host UIs to display them cleanly.

Suggested fields:

- `Alias`
- `Path`
- `Message`

`Path` is a configuration path-like value such as `Processes[1].ScheduledRestarts[0].HourOfDay`.

## Logging Relationship

Diagnostics snapshots answer "what is true now?".

`ILogger` answers "what happened over time?".

The implementation should use both, but diagnostics must not depend on reading logs.

## Thread Safety

`GetSnapshot` should be low-cost and thread-safe. It should not block on long-running lifecycle operations such as process shutdown.

Suggested implementation:

- maintain immutable snapshot state.
- replace the current snapshot atomically after lifecycle or config changes.
- return the latest snapshot immediately.
