# Language & type rules

- **`sealed` by default.** Leave a class open only when it is explicitly a base
  class.
- **`record` for data** (DTOs, options, value objects, event payloads), using
  positional / primary-constructor syntax and `init` setters:

  ```csharp
  public record OpcUaValue(object? Data, TagState State, DateTime TimeStamp);

  public record HomePageOptions : OptionsBase
  {
      public string ConnectionString { get; init; } = string.Empty;
      public string? ApplicationInstance { get; init; }
  }
  ```

  Derive copies with `with`: `opcUaValue with { State = TagState.Failed }`.
- **`required` + `init`** for mandatory immutable properties.
- **Expression-bodied members** for one-liners:

  ```csharp
  public DateTime UtcNow => DateTime.UtcNow;
  public static NavigationHierarchy CreateRoot()
      => new(Root, parentName: null, LearnIcons.Default);
  ```

- **`var` only when the type is obvious** from the right-hand side; explicit type
  otherwise.
- **Collection expressions**: `[]` empty, `[.. source]` to materialise.

  ```csharp
  public ImmutableArray<string> GetClientNames() => [.. ClientsByName.Keys];
  private ImmutableArray<string> AvailableClients { get; set; } = [];
  ```

- Prefer **switch expressions / pattern matching** over if-else ladders.
- **`nameof(...)`** instead of string literals for member/section names.
- **`Guid.CreateVersion7()`** for new IDs (not `Guid.NewGuid()`).
- Always pass an explicit **`StringComparison` / `StringComparer`**
  (`Ordinal`, `OrdinalIgnoreCase`) to string and dictionary operations.

## Immutability & collections

- Public/return surfaces expose **immutable** types: `ImmutableArray<T>`,
  `ImmutableDictionary<K,V>`, `IReadOnlyList<T>`, `IEnumerable<T>`.
- Keep mutable state (`List`, `ConcurrentDictionary`) private; no public
  collection setters; prefer `init`-only properties.
