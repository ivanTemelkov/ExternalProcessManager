# Task 001: Add SonarAnalyzer.CSharp Build Enforcement

## Goal

Make the documented SonarAnalyzer.CSharp requirement real in the build. The user
called this "Sonic Analyzer"; use the actual NuGet package
`SonarAnalyzer.CSharp`.

## Required Reading

- `AGENTS.md`
- `CODING-STYLE.md`
- `docs/aot-readiness.md`
- `docs/implementation-plan/improvements/TASKS.md`

## Implementation Steps

1. Add `SonarAnalyzer.CSharp` as an analyzer package.
2. Prefer a root `Directory.Build.props` so the analyzer covers the library,
   tests, and samples consistently.
3. Use analyzer-only metadata:

   ```xml
   <PackageReference Include="SonarAnalyzer.CSharp" Version="10.27.0.140913" PrivateAssets="all" />
   ```

4. Keep existing AOT, trim, nullable, and warning-as-error settings intact.
5. Build the solution and fix any Sonar warnings without broad suppressions.
6. If a suppression is unavoidable, keep it local and add a clear justification.
7. Record the analyzer version and any warning decisions in
   `../memory.md`.
8. Add a dated progress note to `../progress.md`.

## Done Means

- SonarAnalyzer.CSharp participates in local builds.
- The full solution builds warning-clean.
- No broad `NoWarn` or global suppression is introduced.
- Analyzer setup is easy for a junior developer to find and maintain.

## Validation

Run:

```powershell
dotnet build IvTem.ExternalProcessManager.slnx
dotnet test IvTem.ExternalProcessManager.slnx
rg "SonarAnalyzer.CSharp|NoWarn|SuppressMessage" .
```

Verify:

- Build and tests pass.
- `SonarAnalyzer.CSharp` appears in the expected project or props file.
- Any `NoWarn` or `SuppressMessage` result is narrow and justified.

## Stage And Commit

Run:

```powershell
git status --short
git add Directory.Build.props docs/implementation-plan/progress.md docs/implementation-plan/memory.md
git commit -m "Add SonarAnalyzer build enforcement"
```

If the package reference is added somewhere other than `Directory.Build.props`,
stage that exact file instead.

## Notes For Junior Developer

- Do not install a Sonar scanner unless specifically requested; this task is
  about the analyzer package in the build.
- Do not lower warning severity to make the build pass.
- Keep dependency changes limited to analyzer enforcement.
