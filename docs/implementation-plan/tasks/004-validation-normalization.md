# Task 04: Validation And Effective Configuration Normalization

## Goal

Convert raw configuration into immutable internal effective configuration records and structured validation errors.

## Implementation Steps

1. Add internal effective records for:
   - process config
   - restart config
   - schedule config
2. Validate alias:
   - required
   - non-empty
   - unique case-insensitively
3. Validate file name:
   - required
   - non-empty
4. Parse restart mode:
   - `NonZeroExitCode`
   - `Always`
   - `Never`
5. Apply restart defaults when fields are missing.
6. Parse duration seconds fields as positive integers.
7. Validate positive durations where required.
8. Validate `MinBackoffSeconds <= MaxBackoffSeconds`.
9. Normalize arguments:
   - use `ArgumentList` when non-empty
   - otherwise use raw `Arguments`
10. Return both valid effective configs and invalid entries with validation errors.

## Done Means

- Runtime components can consume only validated effective configs.
- Validation errors include alias when known, configuration path, and message.
- Duplicate aliases are detected case-insensitively.
- Defaults match the docs.

## Test Plan

Unit tests:

- Valid minimal entry becomes effective config.
- Duplicate aliases fail.
- Missing alias fails.
- Missing file name fails.
- Invalid restart mode fails.
- Invalid duration seconds value fails.
- Zero or negative backoff fails.
- `MinBackoffSeconds` greater than `MaxBackoffSeconds` fails.
- Defaults are applied.
- `ArgumentList` is preferred over `Arguments`.

Run:

```powershell
dotnet test
```

## Notes For Junior Developer

- Keep this code deterministic and independent of process launching.
- Avoid throwing for user configuration errors; return validation errors instead.
