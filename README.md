# IvTem.ExternalProcessManager Documentation

## Purpose

IvTem.ExternalProcessManager is a Windows-only .NET 10 class library for host applications that need to supervise external executables. The library reads process definitions from host configuration, starts one process per configured executable, monitors process exits, restarts failed or killed processes according to policy, supports scheduled restarts, and exposes simple diagnostics snapshots for host UI or health views.

The implementation must be compatible with Native AOT and trimming. The library itself is not a native executable; it is a class library that can be referenced by Native AOT-ready hosts.

## Primary Design Decisions

- Target `net10.0`.
- Use `IvTem.ExternalProcessManager` as the solution name, root namespace, and package identity.
- Optimize v1 for Windows only.
- Support both Microsoft.Extensions.Hosting integration and manual manager control.
- Read configuration through `IConfiguration` and `IOptionsMonitor`.
- Support hot reload by diffing configuration changes and applying only required process lifecycle changes.
- Use aliases as the public identity for configured executables.
- Restart behavior is configurable per process.
- Default restart behavior is `NonZeroExitCode`.
- Scheduled restarts support multiple local-time schedules per process.
- Graceful shutdown uses Windows console control events where possible, then process-tree kill after timeout.
- Expose diagnostics as snapshots, not public runtime events.
- Use `ILogger` for lifecycle, validation, and failure logs.

## Documentation Index

- [Configuration](configuration.md): configuration schema, examples, validation rules, and hot reload behavior.
- [Host Integration](host-integration.md): DI registration, hosted service usage, and manual API usage.
- [Process Lifecycle](process-lifecycle.md): start, stop, restart, cleanup, backoff, and Windows shutdown behavior.
- [Scheduled Restarts](scheduled-restarts.md): schedule model, day parsing, and local-time semantics.
- [Diagnostics](diagnostics.md): snapshot model for host applications.
- [AOT Readiness](aot-readiness.md): implementation rules for Native AOT and trimming compatibility.
- [Testing](testing.md): unit and integration test strategy.

## Planned Project Layout

```text
IvTem.ExternalProcessManager.slnx
src/
  IvTem.ExternalProcessManager/
tests/
  IvTem.ExternalProcessManager.Tests/
  IvTem.ExternalProcessManager.TestProcess/
docs/
  README.md
  configuration.md
  host-integration.md
  process-lifecycle.md
  scheduled-restarts.md
  diagnostics.md
  aot-readiness.md
  testing.md
```

## Success Criteria

- A host app can configure multiple external executables by alias.
- The library starts all valid configured processes.
- A failed or killed process restarts according to its configured restart mode and backoff policy.
- Scheduled restarts occur at configured local times.
- Configuration hot reload adds, removes, restarts, or preserves processes correctly.
- Invalid hot-reloaded entries do not bring down the whole manager.
- Host apps can query a diagnostics snapshot that reflects both configuration and runtime state.
- The project builds with AOT and trim analyzers enabled.
