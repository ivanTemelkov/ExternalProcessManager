# AOT Readiness

## Goal

The library must be suitable for host applications that publish with Native AOT and trimming. The library itself is a `net10.0` class library, not a native executable.

## Project Settings

The library project should enable AOT and trimming analyzers.

Suggested properties:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <IsAotCompatible>true</IsAotCompatible>
  <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
  <EnableAotAnalyzer>true</EnableAotAnalyzer>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

Exact properties should be verified during implementation against the installed .NET 10 SDK.

## Configuration Binding

Avoid reflection-heavy runtime binding patterns when practical.

Preferred approach:

- read required sections explicitly from `IConfiguration`.
- parse strings, arrays, dictionaries, and durations manually.
- map into immutable internal records.
- validate during parsing.

This keeps behavior clear and reduces Native AOT surprises.

## Public Types

Public models should be simple records or classes with explicit properties. Avoid requiring reflection-based serialization for core behavior.

Do not require hosts to use a particular JSON serializer for diagnostics.

## Avoided Patterns

Avoid:

- dynamic code generation.
- unbounded reflection over assemblies.
- late-bound activation by type name.
- runtime expression compilation.
- reflection-only configuration binding as the only configuration path.

## Windows Interop

Windows-specific process control can use P/Invoke where needed. Keep P/Invoke declarations internal and small.

All Windows-specific types should be hidden behind internal abstractions so public APIs remain clean even though v1 supports Windows only.

## Analyzer Discipline

Implementation is not complete until:

- normal build passes.
- test build passes.
- AOT and trim analyzer warnings are reviewed and resolved.

Warnings should not be suppressed unless the code path is proven safe and the suppression includes a clear justification.
