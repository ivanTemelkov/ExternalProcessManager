# Implementation Progress

Use this file to track task implementation status.

Statuses:

- `Not Started`
- `In Progress`
- `Implemented`
- `Blocked`
- `Needs Review`

| ID | Task | Status | Notes |
|---|---|---|---|
| 01 | Scaffold solution and projects | Implemented | Uses `IvTem.ExternalProcessManager.slnx` per project direction. |
| 02 | Define public contracts | Implemented | Public manager contract, diagnostics snapshots, status enum, and DI extension added. |
| 03 | Add configuration reader and raw models | Implemented | Raw reader preserves values and configuration paths. |
| 04 | Add validation and effective configuration normalization | Implemented | Produces effective configs plus invalid entries and validation errors. |
| 05 | Implement day and schedule parsing | Not Started | |
| 06 | Implement scheduler calculation | Not Started | |
| 07 | Implement restart backoff policy | Not Started | |
| 08 | Implement process launcher abstraction | Not Started | |
| 09 | Implement Windows cleanup abstraction | Not Started | |
| 10 | Implement per-alias supervisor | Not Started | |
| 11 | Implement manager reconciliation and hot reload | Not Started | |
| 12 | Implement diagnostics snapshots | Not Started | |
| 13 | Implement scheduled restart execution | Not Started | |
| 14 | Add hosted-service integration | Not Started | |
| 15 | Add manual lifecycle idempotency and disposal | Not Started | |
| 16 | Build integration test helper executable | Not Started | |
| 17 | Add logging coverage | Not Started | |
| 18 | Run final analyzer and platform pass | Not Started | |

## Progress Notes

Add dated notes below as implementation progresses.

```text
YYYY-MM-DD:
- Task:
- Change:
- Verification:
- Follow-up:
```

2026-06-30:
- Task: 01 - Scaffold solution and projects.
- Change: Created `IvTem.ExternalProcessManager.slnx`, `src/IvTem.ExternalProcessManager`, `tests/IvTem.ExternalProcessManager.Tests`, and `tests/IvTem.ExternalProcessManager.TestProcess`; added projects to the solution; added the test project reference to the library; enabled AOT, trim, AOT analyzer, and warning-as-error settings on the library; removed generated placeholder source files; updated docs to use `.slnx`.
- Verification: `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 02.

2026-06-30:
- Task: 01 - Scaffold solution and projects.
- Change: Renamed the scaffolded solution and projects to use the `IvTem.ExternalProcessManager` prefix: `IvTem.ExternalProcessManager.slnx`, `src/IvTem.ExternalProcessManager`, `tests/IvTem.ExternalProcessManager.Tests`, and `tests/IvTem.ExternalProcessManager.TestProcess`; updated project references and documentation.
- Verification: `dotnet build IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 02.

2026-06-30:
- Task: 02 - Define public contracts.
- Change: Added `IExternalProcessManager`, immutable diagnostics snapshot records, `ExternalProcessStatus`, and `AddExternalProcessManager`; registered an internal placeholder manager until lifecycle implementation tasks replace it; added compile-oriented public contract tests.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 03.
- Memory: Added decisions for immutable snapshot collections and the temporary internal manager registration.

2026-06-30:
- Task: 03 - Add configuration reader and raw models.
- Change: Added internal raw configuration records and `ExternalProcessConfigurationReader` that manually reads `Processes`, launch fields, argument arrays, environment children, restart settings, scheduled restart settings, and configuration paths from `IConfiguration`.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 04.
- Memory: Added decisions for path-preserving raw values and scalar/array day-of-week reading.

2026-06-30:
- Task: 04 - Add validation and effective configuration normalization.
- Change: Added internal effective configuration records, restart and argument-mode enums, invalid-entry records, and `ExternalProcessConfigurationValidator`; validation covers required/unique aliases, required file names, restart mode parsing, duration parsing, positive duration checks, `MinBackoff <= MaxBackoff`, defaults, environment normalization, and `ArgumentList` preference.
- Verification: `dotnet test IvTem.ExternalProcessManager.slnx` succeeded with 0 warnings and 0 errors.
- Follow-up: Continue with Task 05.
- Memory: Added decisions for duplicate-alias handling and carrying scheduled restart values forward unparsed until Task 05.
