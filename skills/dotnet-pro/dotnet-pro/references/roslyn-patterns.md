# Roslyn Analysis Patterns for TestWire

## Safe Syntax-to-Symbol Resolution
When moving from `SyntaxNode` to `ISymbol`, always check for nulls:
```csharp
var semanticModel = compilation.GetSemanticModel(syntaxTree);
var symbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
if (symbol == null) return;
```

## Generic Type Unwrapping
TestWire needs the underlying type from `Task<T>`, `ActionResult<T>`, etc. Use recursive unwrapping:
```csharp
private static string Unwrap(ITypeSymbol symbol) {
    if (symbol is INamedTypeSymbol named && (named.Name == "Task" || named.Name == "ActionResult")) {
        return named.TypeArguments.Length > 0 ? Unwrap(named.TypeArguments[0]) : "void";
    }
    return symbol.ToDisplayString();
}
```

## Attribute Analysis
Avoid string-based attribute matching if possible. Prefer checking the `AttributeClass`.
```csharp
private static bool HasAttribute(ISymbol symbol, string name) =>
    symbol.GetAttributes().Any(a => a.AttributeClass?.Name == name || a.AttributeClass?.Name == name + "Attribute");
```

## MSBuild Workspace
Ensure `MSBuildLocator` is registered before creating `MSBuildWorkspace`.
Always dispose the workspace to free file locks.
