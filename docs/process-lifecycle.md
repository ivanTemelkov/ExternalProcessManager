# Process Lifecycle

## States

Each alias has runtime state. Suggested state names:

- `NotStarted`
- `Starting`
- `Running`
- `Stopping`
- `RestartPending`
- `Stopped`
- `Faulted`
- `InvalidConfiguration`

The exact enum names can change during implementation, but diagnostics must expose enough information for a host UI to display current process health.

## Startup

When the manager starts:

1. Load and validate the current configuration.
2. Start every valid configured process.
3. Record runtime state for invalid entries and expose validation errors.
4. Start schedule monitoring for valid entries with scheduled restarts.

Each process is launched from a validated effective configuration.

## Process Launch

The Windows launcher should:

- create the process with configured file name, arguments, working directory, and environment.
- support structured arguments through `ProcessStartInfo.ArgumentList`.
- use raw `Arguments` only when `ArgumentList` is empty.
- launch the process in a way that allows Windows console control events for graceful shutdown.
- track process ID and start time.

## Launch Failure

If a validated process cannot be launched, the alias moves to `Faulted`, diagnostics set `LastError`, and no process handle is retained.

Launch failures are not treated as process exits in v1. They do not increment `RestartCount`, do not enter `RestartPending`, and do not use restart backoff. Retrying launch failures is out of scope for v1 unless a later task changes the policy.

## Exit Handling

When a process exits:

- capture exit code.
- determine whether the exit was intentional.
- determine whether restart policy requires restart.
- apply restart backoff if restarting.
- update diagnostics.
- log the exit and decision.

An intentional stop is any stop initiated by:

- host shutdown
- alias removal
- configuration change requiring restart
- scheduled restart
- explicit manual stop of the manager

## Restart Modes

`NonZeroExitCode`:

- exit code `0` remains stopped.
- nonzero exit code restarts.
- killed processes normally surface as nonzero or abnormal termination and should restart.

`Always`:

- any non-intentional exit restarts.

`Never`:

- no non-intentional exit restarts.

## Backoff

Backoff prevents crash loops from consuming CPU.

Behavior:

- first restart waits `MinBackoffSeconds`.
- repeated failures grow exponentially.
- delay is capped at `MaxBackoffSeconds`.
- backoff resets after `StableRunDurationSeconds` of continuous runtime.

Backoff state is per alias.

## Graceful Stop

For v1 Windows behavior:

1. Send a console control event such as CTRL+BREAK to the process group when possible.
2. Wait `GracefulStopTimeoutSeconds`.
3. If the process is still alive, kill the process tree.
4. Update diagnostics with the final outcome.

This design is intended for console-style child processes. GUI processes or processes that do not handle console control events may be killed after the timeout.

## Cleanup Scope

Cleanup should terminate the configured process and its descendants. The implementation should prefer reliable Windows process-tree cleanup over root-process-only cleanup.

The cleanup component must be isolated behind an internal abstraction so Windows-specific implementation details do not leak into public APIs.

## Concurrency

Lifecycle operations for the same alias must be serialized. A scheduled restart, config reload, and unexpected exit should not race into duplicate starts.

Suggested approach:

- one supervisor object per alias
- per-alias async lock or serialized command loop
- manager-level reconciliation lock for config changes

## Disposal

All process handles, timers, cancellation token sources, and change subscriptions must be disposed.

`StopAsync` should attempt graceful cleanup for all running managed processes before disposal.
