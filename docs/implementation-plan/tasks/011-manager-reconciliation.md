# Task 11: Manager Reconciliation And Hot Reload

## Goal

Implement the top-level manager that reconciles configuration changes with running supervisors.

## Implementation Steps

1. Implement `IExternalProcessManager`.
2. On `StartAsync`, read and validate current configuration.
3. Start supervisors for valid aliases.
4. Record invalid aliases for diagnostics.
5. Subscribe to configuration change tokens or `IOptionsMonitor`-equivalent changes.
6. On change, diff by case-insensitive alias:
   - added valid alias starts
   - removed alias stops and removes supervisor
   - changed valid alias stops old process and starts new config
   - unchanged alias is preserved
   - invalid new alias is skipped
   - invalid changed alias keeps last valid running config
7. Use a manager-level reconciliation lock.
8. Ensure one invalid entry does not block other valid changes.

## Done Means

- Manager applies current config on start.
- Manager applies hot reload best effort.
- Existing valid processes survive unrelated invalid config entries.
- Removed aliases are stopped and removed from runtime state.

## Test Plan

Unit tests with fake supervisors or fake launcher:

- Added valid alias starts.
- Removed alias stops.
- Changed alias restarts.
- Unchanged alias is preserved.
- Invalid new alias is not started and appears in diagnostics.
- Invalid changed alias keeps existing process running and appears in diagnostics.
- Multiple changes apply best effort.

Run:

```powershell
dotnet test
```

## Notes For Junior Developer

- Do not let validation exceptions escape to the host for normal bad config.
- Avoid holding the manager lock while doing long process shutdown if possible; if unavoidable, keep snapshot access non-blocking.
