# Async & concurrency rules

- Async methods return **`Task` / `Task<T>`** (or `ValueTask` on hot paths).
  **Do not add an `Async` suffix** — name the method for what it does. Add the
  suffix only to disambiguate a sync/async pair of the same operation (rare).
  **Never `async void`** except unavoidable event handlers.
- **Append `.ContinueOnAnyContext()` to every awaited call.** This is the
  project wrapper for `ConfigureAwait(continueOnCapturedContext: false)`. Do
  **not** write `ConfigureAwait(false)` longhand.

  ```csharp
  var result = await session.SimpleRead(nodeId, Attributes.Value)
      .ContinueOnAnyContext();
  await app.RunAsync().ContinueOnAnyContext();
  ```

  > The wrappers can be found in the LinkManagement.Common.Tasks namespace of the LinkManagement.Common project

- **Flow `CancellationToken`** through async APIs and honour it.
- Guard shared state with `lock` over a `private static readonly Lock`
  (the .NET `System.Threading.Lock` type).
- Dispose every `CancellationTokenSource` you create (cancel + dispose in
  `Dispose`).
