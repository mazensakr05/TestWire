# .NET 8 & C# 12 Idioms

## C# 12 Features
- **Primary Constructors**: Use `public class MyService(IDependency dependency)` for cleaner DI.
- **Collection Expressions**: Use `int[] numbers = [1, 2, 3];` instead of `new[] { 1, 2, 3 }`.
- **Default Lambda Parameters**: `var lambda = (string name = "Guest") => ...`.

## .NET 8 Performance & Best Practices
- **Frozen Collections**: Use `ToFrozenDictionary()` and `ToFrozenSet()` for read-only lookup data.
- **SearchValues<T>**: Use for efficient searching within spans.
- **Keyed DI**: Use `[FromKeyedServices("myKey")]` for resolving specific implementations.

## Global Usings & File-Scoped Namespaces
Always prefer file-scoped namespaces: `namespace MyProject.Analysis;` to reduce indentation.
Use `GlobalUsings.cs` for common namespaces like `System`, `System.Collections.Generic`, and `Microsoft.CodeAnalysis`.
