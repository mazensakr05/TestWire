---
name: unit-test-pro
description: Specializes in designing and implementing high-quality unit and integration tests for .NET applications using xUnit, NUnit, and Moq. Use this skill when the user wants to add, improve, or troubleshoot tests, especially for ASP.NET Core controllers and services.
---

# Unit Test Pro Skill

This skill provides expert guidance for testing .NET applications effectively.

## Core Workflows

### 1. Designing Integration Tests
- Use `WebApplicationFactory<TEntryPoint>` to spin up an in-memory test server.
- Configure services in `ConfigureTestServices` to swap real implementations for mocks where necessary.
- Use `HttpClient` for end-to-end behavioral validation.

### 2. Mocking Patterns (Moq)
- **Strict vs Loose**: Prefer Loose mocks by default.
- **Behavior Setup**: Use `Setup(...).ReturnsAsync(...)` for async methods.
- **Verifying Calls**: Use `Verify(m => m.Method(), Times.Once)` to ensure critical side effects occurred.

### 3. Writing Clean Assertions
- **FluentAssertions**: Recommend using FluentAssertions for readable checks like `result.Should().BeEquivalentTo(expected)`.
- **Status Codes**: Always assert `HttpStatusCode` for API tests.

## Best Practices
- **Arrange-Act-Assert (AAA)**: Clearly separate the three phases of every test.
- **Test Naming**: Use `MethodName_ShouldExpectedBehavior_WhenStateUnderTest` pattern.
- **Data-Driven Tests**: Use `[Theory]` and `[InlineData]` in xUnit to cover multiple scenarios with one method.
- **Avoid Over-Mocking**: Don't mock private methods or internal logic; test through the public API.
