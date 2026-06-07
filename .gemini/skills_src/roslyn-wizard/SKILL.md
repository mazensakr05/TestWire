---
name: roslyn-wizard
description: Expert guidance for Roslyn-based code analysis and generation. Use this skill when working on projects that involve SemanticModels, SyntaxTrees, MSBuildWorkspace integration, or custom diagnostic reporting in .NET.
---

# Roslyn Wizard Skill

This skill provides specialized knowledge for static analysis and code generation using the .NET Compiler Platform (Roslyn).

## Core Workflows

### 1. Navigating Syntax Trees
- Use `DescendantNodes().OfType<T>()` to find specific code elements.
- Understand the difference between `SyntaxNode` (the structure) and `ISymbol` (the meaning).
- Use `semanticModel.GetDeclaredSymbol(node)` to cross from syntax to semantics.

### 2. Resolving Types Semantically
- Use `INamedTypeSymbol` to inspect class properties, constructors, and attributes.
- Handle generic types by inspecting `typeArguments` on `INamedTypeSymbol`.
- Always verify if a symbol is `ErrorType` before proceeding to avoid crashes on uncompiled code.

### 3. MSBuildWorkspace Integration
- Initialize with `MSBuildLocator.RegisterDefaults()` before creating the workspace.
- Handle `WorkspaceFailed` events to diagnose project loading issues (missing SDKs, NuGet errors).
- Use `OpenProjectAsync` or `OpenSolutionAsync` for full type resolution across dependencies.

## Best Practices
- **Immutable Trees**: Remember that Roslyn syntax trees are immutable; use `WithXxx` methods to create modified versions.
- **Performance**: Cache `SemanticModel` instances when analyzing multiple nodes in the same file.
- **Minimally Qualified Names**: Use `SymbolDisplayFormat.MinimallyQualifiedFormat` when generating strings intended for user-facing code to keep it readable.
