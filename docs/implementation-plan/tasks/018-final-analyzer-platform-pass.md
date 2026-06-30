# Task 18: Final Analyzer And Platform Pass

## Goal

Verify the implementation is buildable, testable, Windows-explicit, and AOT/trim friendly.

## Implementation Steps

1. Run full build.
2. Run full tests.
3. Review AOT analyzer warnings.
4. Review trim analyzer warnings.
5. Fix warnings where possible.
6. If suppression is unavoidable, keep it narrow and add a clear justification.
7. Verify non-Windows behavior is explicit.
8. Verify public API remains compact.
9. Verify no internal implementation types leaked into public surface.

## Done Means

- `dotnet build` passes.
- `dotnet test` passes.
- AOT and trim warnings are resolved or justified.
- Windows-only behavior is clear.
- The implementation satisfies the acceptance scenarios in `../TASKS.md`.

## Test Plan

Run:

```powershell
dotnet build
dotnet test
```

Manual review:

- Public API review.
- Analyzer suppression review.
- Platform guard review.
- Diagnostics snapshot review.

## Notes For Junior Developer

- Do not mark v1 complete if analyzer warnings are ignored without justification.
- Add final gotchas or decisions to `../memory.md`.
