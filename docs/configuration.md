# Configuration

## Source

Configuration is supplied by the host application through `IConfiguration`, normally from `appsettings.json` with reload-on-change enabled. The library should not own a separate file watcher in v1. Hot reload is driven by `IOptionsMonitor` or an equivalent change-token based adapter.

Default root section:

```json
{
  "ExternalProcessManager": {
    "Processes": []
  }
}
```

## Process Entry Schema

Each configured process is identified by `Alias`.

```json
{
  "Alias": "worker-a",
  "FileName": "C:\\apps\\worker-a.exe",
  "Arguments": "--port 5050",
  "ArgumentList": ["--port", "5050"],
  "WorkingDirectory": "C:\\apps",
  "Environment": {
    "DOTNET_ENVIRONMENT": "Production"
  },
  "Restart": {
    "Mode": "NonZeroExitCode",
    "MinBackoffSeconds": 2,
    "MaxBackoffSeconds": 60,
    "StableRunDurationSeconds": 300,
    "GracefulStopTimeoutSeconds": 10
  },
  "ScheduledRestarts": [
    { "HourOfDay": "23:45", "DayOfWeek": "All" },
    { "HourOfDay": "04:00", "DayOfWeek": "Monday|Friday" }
  ]
}
```

## Required Fields

- `Alias`: unique, non-empty, case-insensitive process identity.
- `FileName`: executable path or executable name resolvable by Windows process start rules.

## Optional Fields

- `Arguments`: raw command-line argument string.
- `ArgumentList`: structured argument array. Prefer this when both `Arguments` and `ArgumentList` are configured.
- `WorkingDirectory`: process working directory.
- `Environment`: additional or overridden environment variables.
- `Restart`: restart behavior and backoff settings.
- `ScheduledRestarts`: zero or more scheduled restart definitions.

## Restart Configuration

`Restart.Mode` values:

- `NonZeroExitCode`: restart when the process exits with a nonzero exit code. This is the default.
- `Always`: restart after any exit unless the manager intentionally stopped the process.
- `Never`: never restart after exit.

Backoff defaults should be conservative:

- `MinBackoffSeconds`: `2`
- `MaxBackoffSeconds`: `60`
- `StableRunDurationSeconds`: `300`
- `GracefulStopTimeoutSeconds`: `10`

Backoff uses exponential growth from `MinBackoffSeconds` to `MaxBackoffSeconds`. It resets after the process stays alive for at least `StableRunDurationSeconds`.

## Scheduled Restart Configuration

Each scheduled restart entry has:

- `HourOfDay`: required `HH:mm` local host time.
- `DayOfWeek`: required day selector.

Accepted `DayOfWeek` forms:

- `All`
- `Monday`
- `Monday,Friday`
- `Monday|Friday`
- JSON array such as `["Monday", "Friday"]`

Day names are culture-invariant English names.

## Validation Rules

Configuration validation should report errors without crashing the host.

Invalid conditions include:

- Missing or duplicate alias.
- Missing executable file name.
- Invalid restart mode.
- Invalid restart duration seconds values.
- Invalid `HourOfDay` format.
- Invalid day-of-week value.
- Negative or zero backoff values where a positive duration is required.
- `MinBackoffSeconds` greater than `MaxBackoffSeconds`.

## Hot Reload Behavior

On configuration change, the manager performs a diff by alias.

- Added valid alias: start the process.
- Removed alias: gracefully stop and remove runtime state.
- Changed valid alias: gracefully stop old process, then start with the new effective configuration.
- Unchanged alias: keep running without interruption.
- Invalid new alias: do not start it; expose validation errors in diagnostics.
- Invalid changed alias: keep the last valid running configuration; expose validation errors in diagnostics.

Hot reload is best effort. One invalid entry must not prevent other valid entries from being applied.

## Effective Configuration

The implementation should normalize configuration into immutable internal records before applying it. The normalized model should contain:

- resolved alias comparison key
- file name
- argument mode and values
- working directory
- environment overrides
- restart mode and resolved defaults
- parsed scheduled restart definitions

Runtime components should consume only validated effective configuration.
