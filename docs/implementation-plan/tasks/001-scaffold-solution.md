# Task 01: Scaffold Solution And Projects

## Goal

Create the repository structure described by the docs:

- `IvTem.ExternalProcessManager.slnx`
- `src/IvTem.ExternalProcessManager`
- `tests/IvTem.ExternalProcessManager.Tests`
- `tests/IvTem.ExternalProcessManager.TestProcess`

## Implementation Steps

1. Create a .NET XML solution named `IvTem.ExternalProcessManager.slnx`.
2. Create a class library project named `IvTem.ExternalProcessManager` targeting `net10.0`.
3. Create an xUnit test project named `IvTem.ExternalProcessManager.Tests` targeting `net10.0`.
4. Create a console project named `IvTem.ExternalProcessManager.TestProcess` targeting `net10.0`.
5. Add all projects to the solution.
6. Add a test project reference from `IvTem.ExternalProcessManager.Tests` to `IvTem.ExternalProcessManager`.
7. Add a test project reference from `IvTem.ExternalProcessManager.Tests` to the test helper only if needed for locating build output; otherwise keep helper independent.
8. Configure the library project with:
   - `IsAotCompatible`
   - `EnableTrimAnalyzer`
   - `EnableAotAnalyzer`
   - `TreatWarningsAsErrors`
9. Keep generated placeholder classes minimal or remove them once real files exist.

## Done Means

- Solution and three projects exist in the expected folders.
- All projects target `net10.0`.
- The library has AOT and trim analyzers enabled.
- The test project references the library.
- The solution builds with no source implementation beyond scaffolding.

## Test Plan

Run:

```powershell
dotnet build
```

Expected result:

- Restore succeeds.
- Build succeeds.
- No analyzer warnings are emitted by the empty scaffold.

## Notes For Junior Developer

- Do not add implementation code in this task.
- Do not change the docs.
- If the SDK version complains about `net10.0`, stop and record the issue in `../memory.md`.
