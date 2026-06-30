# Naming rules

| Element | Rule | Example |
|---|---|---|
| Class / record / struct / method / property | `PascalCase` | `OpcUaClientManager`, `ReadTag` |
| Interface | `I` + `PascalCase` | `IOpcUaClient` |
| Constant / `static readonly` | `PascalCase` | `StorageKey` |
| Parameter / local | `camelCase` | `connectionString` |
| **Private backing field** | **`camelCase`, NO `_` prefix** | `private string? selectedClientName;` |
| Generic type param | `T` or `T`-prefixed | `T`, `TPresentedType` |
| Async method | **No `Async` suffix** (see below) | `ReadTag`, `Connect` |

Key points:

- **Never use `_` prefixed fields.** Backing fields are plain `camelCase`.
- **Prefer a private auto-property over a field**, even `PascalCase` private
  properties for injected dependencies and internal state:

  ```csharp
  private ILogger<OpcUaClient> Logger { get; }
  private Session? Session { get; set; }
  private ConcurrentDictionary<OpcUaNodeId, MonitoredOpcUaTag> MonitoredTagsByNodeId { get; } = new();
  ```

  Use an explicit `camelCase` backing field only when get/set needs logic.
- **Do not append an `Async` suffix.** Async methods are named for what they do
  (`ReadTag`, `Connect`, `LoadFilterFromStorage`). Add the suffix only to
  distinguish a synchronous and asynchronous overload of the same operation
  (e.g. `Save` / `SaveAsync`) — and that is rare. External frameworks may impose
  their own names (e.g. `RunAsync`).
- Folder/namespace names reuse the project vocabulary: `Feature`, `Definition`,
  `Model`, `Extensions`, `Interfaces`, `Exceptions`, `Configuration`,
  `Repository`, `Controllers`, `Attributes`.
