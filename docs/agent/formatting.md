# Formatting & file layout rules

- **File-scoped namespaces only**: `namespace LinkManagement.Common.Attributes;`
- **Namespace mirrors folder path** exactly.
- **One public type per file**, file named after the type. A small tightly-
  coupled helper type may share the file.
- `using` directives at the **top**, above the namespace. Shared imports may go
  in a project `GlobalUsings.cs` (`global using ...;`); the app uses
  `<ImplicitUsings>enable</ImplicitUsings>`. **No file headers/copyright.**
- **4-space indentation**, no tabs.
- **Allman braces** (opening brace on its own line).
- Single-statement `if`/`else`/loop body: **no braces**, placed on the next line:

  ```csharp
  if (firstRender == false)
      return;
  ```

- Fluent / LINQ chains break **before** the `.`, one call per line:

  ```csharp
  var index = configuration.LoggingRules
      .Select((rule, i) => (i, rule))
      .Where(x => x.rule.LoggerNamePattern.Equals("*", StringComparison.Ordinal))
      .Select(x => x.i)
      .FirstOrDefault();
  ```

- **No `this.` qualifier.**
- Negate booleans with **`== false`**, not `!`:
  `if (CheckConnection(out var session) == false)`.
