# AGENTs.md — Coding rules for AI agents

Authoritative coding-style instructions for AI agents working in the current project.
Platform: **.NET 10 / C# latest**. Nullable enabled. **SonarAnalyzer.CSharp**
runs in the build — produce warning-clean code.

Human-readable rationale and examples live in `CODING-STYLE.md`. This file is
optimized for agents: terse rules first, deep detail loaded on demand.

## Project context

- This is a Windows-only `net10.0` class library for supervising configured
  external processes.
- The library must remain suitable for Native AOT and trimming.
- Build output must stay SonarAnalyzer-clean and warning-clean.
- Configuration is supplied by host `IConfiguration` / options flow; do not add
  a separate file watcher in v1.
- Hot reload is best effort: invalid entries must not crash the host or block
  valid entries, and invalid changed entries keep the last valid running
  configuration.
- Diagnostics are snapshots for host UI or health views, not public runtime
  events.

## Project documentation routing

- Read `docs/README.md` for the system overview, success criteria, and planned
  layout before broad implementation work.
- For configuration, lifecycle, scheduling, diagnostics, host integration, AOT,
  or testing changes, read the matching domain doc in `docs/`.
- For implementation-plan work, read `docs/implementation-plan/TASKS.md`, then
  read the matching task file in `docs/implementation-plan/tasks/`.

## Implementation plan workflow

- Implement tasks in `docs/implementation-plan/TASKS.md` order unless a task
  explicitly says it can be done independently.
- Before starting an implementation task, check
  `docs/implementation-plan/progress.md` and
  `docs/implementation-plan/memory.md`.
- After each implementation, update `docs/implementation-plan/progress.md` with
  the task status, dated change summary, verification performed, and follow-up.
- After each implementation, update `docs/implementation-plan/memory.md` with
  newly discovered requirements, gotchas, decisions, or debugging notes.
- If no new memory entry is needed, state that in the progress note.

## How to use this file (token-efficient)

1. Apply the **Always-on rules** below to every change — they are short on
   purpose, keep them in context.
2. Load a detail file from `docs/agent/` **only when your task touches that
   area** (see the routing table). Do not read files you don't need.

| If your task involves... | Read |
|---|---|
| Naming anything | `docs/agent/naming.md` |
| Layout, braces, whitespace, namespaces | `docs/agent/formatting.md` |
| Classes/records, `var`, generics, language features | `docs/agent/language.md` |
| `async`/`await`, `Task`, threads, cancellation | `docs/agent/async.md` |
| Registering services, `ILogger`, logging | `docs/agent/di-logging.md` |
| Nullability rules | `docs/agent/nullability.md` |

## Always-on rules (apply to every file)

- File-scoped namespace; namespace **mirrors the folder path**; **one public
  type per file** named after the type.
- `using`s at top of file. Don't add a file header/copyright.
- 4-space indent, **Allman braces**. Single-statement bodies: no braces, on the
  next line. Break fluent chains before each `.`.
- **No `this.` qualifier.** Negate with **`== false`**, never `!`.
- Types are **`sealed`** unless meant as a base class. Use **`record`** for data
  (DTOs, options, value objects, event args) with `init` / `required`.
> When records are used for configuration options the proerties hsould be set and not init because of the binding
- Private backing fields are **`camelCase` with no `_` prefix**; prefer a
  private auto-property over a field. (`naming.md`)
- `var` only when the type is obvious from the right-hand side.
- Every awaited call ends with **`.ConfigureAwait(continueOnCapturedContext: false)`** in library calls; async methods
  return `Task`/`ValueTask`; never `async void`. **Do not add an `Async`
  suffix** — only to disambiguate a sync/async pair (rare). (`async.md`)
- Custom exceptions are `sealed` in `Exceptions/`. (`errors-and-null.md`)
- Nullable is on: annotate with `[NotNull]` / `[NotNullWhen(true)]`, guard with
  `ArgumentNullException.ThrowIfNull(...)`. (`errors-and-null.md`)
- Always pass an explicit **`StringComparison` / `StringComparer`** to string &
  dictionary operations.
- Prefer immutable public APIs and keep the mutable state private.
- Register services via fluent `Add{Feature}` extension methods on
  `IServiceCollection`; constructor-inject; log via `ILogger<T>` with structured
  templates. (`di-logging.md`)
- Use `Guid.CreateVersion7()` and `nameof(...)`; prefer switch expressions &
  pattern matching.

## Hard constraints

- **Match existing surrounding style** over any external "best practice".
- **Do not** introduce EF Core, `_field` naming, blanket `Async` suffixes, `!`-negation, or block-scoped
  namespaces.
- Keep the build **Sonar-warning-clean**.
- When unsure, open the nearest existing file in the same folder and copy its
  patterns.
- When planning keep asking questions until you are 95% sure that you understand what I want.
- When implementing keep trying until you are 95% confident of the correctness.
