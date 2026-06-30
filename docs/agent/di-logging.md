# Dependency injection & logging rules

## Service registration

- Register through **fluent extension methods on `IServiceCollection`**, named
  `Add{Feature}`, returning `IServiceCollection` so calls chain. Place them in a
  `ServiceCollectionExtensions` class in the feature's namespace.

  ```csharp
  public static IServiceCollection AddLearn(this IServiceCollection sc)
      => sc.AddSingleton<DocumentDescriptors>(_ => DocumentDescriptors.Create())
          .AddSingleton<INavigationHierarchyProvider, NavigationHierarchyProvider>()
          .AddScoped<NavigationDispatcher>();
  ```

- **Constructor injection only.** Store dependencies in private `{ get; }`
  properties.

## Logging

- Use **`Microsoft.Extensions.Logging`** abstractions (`ILogger<T>`); **NLog** is
  the configured provider.
- Use **structured message templates** — never string interpolation — in log
  calls: `Logger.LogWarning("Invalid syntax! {Syntax}", syntax);`.
- For hot/structured paths use the **`[LoggerMessage]` source-generated** pattern:

  ```csharp
  [LoggerMessage(LogLevel.Information, "OPC-UA Client {ClientName} ({Guid}) connected.")]
  static partial void LogConnected(ILogger logger, string clientName, Guid guid);
  ```
