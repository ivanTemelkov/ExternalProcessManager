# Async & concurrency rules

- Async methods return **`Task` / `Task<T>`** (or `ValueTask` on hot paths).
  **Do not add an `Async` suffix** — name the method for what it does. Add the
  suffix only to disambiguate a sync/async pair of the same operation (rare).
  **Never `async void`** except unavoidable event handlers.
- **Append `.ConfigureAwait(continueOnCapturedContext: false)` to every awaited call in libraries.**

  ```csharp
  var result = await session.SimpleRead(nodeId, Attributes.Value)
      .ConfigureAwait(continueOnCapturedContext: false);
  await app.RunAsync()
      .ConfigureAwait(continueOnCapturedContext: false);  
  ```

  > The wrappers can be found in the LinkManagement.Common.Tasks namespace of the LinkManagement.Common project

- **Flow `CancellationToken`** through async APIs and honour it.
- Guard shared state with `lock` over a `private static readonly Lock`
  (the .NET `System.Threading.Lock` type).
- Dispose every `CancellationTokenSource` you create (cancel + dispose in
  `Dispose`).
