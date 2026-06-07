---
name: dotnet-expert
description: Deep expertise in the .NET ecosystem, including C# 12+ features, ASP.NET Core architecture, dependency injection, and advanced project configuration. Use this skill when the user needs help with complex .NET development tasks, architectural decisions, or troubleshooting SDK-related issues.
---

# .NET Expert Skill

This skill provides specialized knowledge and workflows for high-quality .NET development.

## Core Workflows

### 1. Architectural Review
When reviewing .NET code, focus on:
- **Dependency Injection (DI)**: Ensure interfaces are used for decoupling and services are registered with appropriate lifetimes (Transient, Scoped, Singleton).
- **Options Pattern**: Use `IOptions<T>` for strongly-typed configuration.
- **Middleware and Filters**: Suggest using middleware or Action Filters for cross-cutting concerns (logging, error handling, auth).

### 2. C# Modernization
Proactively suggest modern C# features:
- **Primary Constructors**: `public class MyService(IDependency dep) { ... }`
- **Pattern Matching**: `if (obj is { Property: value })`
- **File-scoped Namespaces**: `namespace MyProject.Services;`
- **Collection Expressions**: `int[] array = [1, 2, 3];`

### 3. Troubleshooting Build & Runtime
- Check `TargetFramework` compatibility.
- Investigate NuGet package conflicts using `dotnet list package --include-transitive`.
- Use `dotnet --info` to verify environment setup.

## Best Practices
- **Async/Await**: Always use `Task.FromResult` or `ValueTask` for synchronous implementations of async interfaces to avoid warnings and context switches.
- **Minimal APIs vs Controllers**: Use Minimal APIs for simple microservices; use Controllers for complex, attribute-heavy business logic.
- **Record Types**: Use `record` or `record struct` for DTOs and immutable data models.
