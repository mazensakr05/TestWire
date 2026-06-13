# Roslyn Mastery: Senior Engineer Reference

Timeless knowledge for building static analysis tools, source generators, and refactorings using the .NET Compiler Platform (Roslyn).

## 1. Syntax Tree vs. Semantic Model
- **Syntax Tree:** Represents the structure of the code (lexical and syntactic). It's "dumb"—it only knows about tokens, nodes, and trivia. Fast but shallow.
- **Semantic Model:** Represents the "meaning" of the code. It answers questions like "What type does this variable have?" or "Where is this method defined?". Slow (requires compilation) but deep.
- **Rule of Thumb:** Use Syntax for structural changes; use Semantic for type-aware analysis.

## 2. Core Symbol Types
Symbols represent the entities defined by the C# language.
- **ITypeSymbol:** Represents a type (Class, Struct, Interface, Enum, etc.).
- **IMethodSymbol:** Represents a method, constructor, or accessor.
- **IPropertySymbol:** Represents a property or indexer.
- **IParameterSymbol:** Represents a method parameter.
- **ISymbol:** The base interface for all symbols.

## 3. Walking Syntax Trees Safely
- **CSharpSyntaxWalker:** Inherit from this to visit specific nodes (e.g., `VisitMethodDeclaration`).
- **Null Checks:** Always check for null when accessing optional nodes (e.g., `methodDeclaration.Body`).
- **Trivia:** Be mindful of "trivia" (whitespace and comments) when rewriting code. Use `.WithLeadingTrivia()` and `.WithTrailingTrivia()` to preserve formatting.

## 4. Attribute Detection
- **Detection:** Use `symbol.GetAttributes()` to find attributes applied to a symbol.
- **AttributeData:** Contains information about the attribute, including `AttributeClass`, `ConstructorArguments`, and `NamedArguments`.
- **Match by Name:** Always use the full metadata name (e.g., `Microsoft.AspNetCore.Mvc.HttpGetAttribute`) when checking for specific attributes.

## 5. Block Body vs. Expression Body
C# methods can have two forms:
- **BlockSyntax:** `{ return 1; }` (found in `method.Body`).
- **ArrowExpressionClauseSyntax:** `=> 1;` (found in `method.ExpressionBody`).
- **Analysis:** You must check BOTH when analyzing method logic.

## 6. Type Resolution
- **GetTypeByMetadataName:** Use `compilation.GetTypeByMetadataName("Namespace.TypeName")` to get a symbol for a known type.
- **GetSymbolInfo:** Use on a syntax node (like `IdentifierNameSyntax`) to get the symbol it refers to.
- **GetDeclaredSymbol:** Use on a declaration node (like `ClassDeclarationSyntax`) to get the symbol it defines.

## 7. Safe Patterns & Performance
- **Stale Compilations:** Always use the latest `Compilation` object. Don't hold onto symbols across compilation updates.
- **DeclaringSyntaxReferences:** A symbol might be defined in multiple files (partial classes). Always check `symbol.DeclaringSyntaxReferences`.
- **String Comparisons:** Use `StringComparer.Ordinal` or `StringComparer.OrdinalIgnoreCase` when comparing symbol names to avoid culture-related bugs.
- **CancellationToken:** Always pass and check the `CancellationToken` in long-running analysis tasks.
