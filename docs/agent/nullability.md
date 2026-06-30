# Nullability rules

- Nullable reference types are **enabled solution-wide**; keep code
  null-warning-free.
- Annotate intent with `System.Diagnostics.CodeAnalysis` attributes:
  `[NotNull]` on parameters, `[NotNullWhen(true)]` on `out` params,
  `[MaybeNull]`.

  ```csharp
  bool TryGetNode(string key, [NotNullWhen(true)] out DependencyTreeNode<T>? node);
  ```

- Guard with framework throw-helpers at method top:
  `ArgumentNullException.ThrowIfNull(x);`,
  `ArgumentException.ThrowIfNullOrEmpty(name);`.
- Use the null-forgiving `!` only where justified (notably Blazor `[Inject]` /
  `[Parameter]` initialisers).
