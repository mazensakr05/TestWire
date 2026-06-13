# C# Mastery: Senior Engineer Reference

Timeless C# principles for high-performance, maintainable, and safe codebases.

## 1. Async/Await & Concurrency
- **State Machine:** Understand that `async` methods are transformed into a state machine. This adds overhead; don't use `async` for trivial pass-through methods (return the `Task` directly).
- **Deadlocks:** In older .NET Framework or UI contexts, `Task.Wait()` or `Task.Result` can deadlock if a `SynchronizationContext` is present. In modern .NET (Core/5+), this is rarer but still a "code smell".
- **ConfigureAwait(false):** Always use in library code to avoid capturing the synchronization context, improving performance and preventing deadlocks.
- **Async Void:** Use ONLY for event handlers. Exceptions in `async void` methods crash the process. Use `Task` for everything else.
- **CancellationToken:** Always propagate `CancellationToken` to downstream async calls. Use `LinkedTokenSource` for timeouts.
- **Task vs. ValueTask:** Use `ValueTask` for methods that often complete synchronously (e.g., reading from a cache or buffer) to reduce heap allocations.

## 2. Null Safety
- **Nullable Reference Types (NRT):** Enable `<Nullable>enable</Nullable>` at the project level. Treat warnings as errors.
- **Null Guards:** Use `ArgumentNullException.ThrowIfNull(param)` for public API boundaries.
- **Safe Navigation:** Use `?.` (null-conditional), `??` (null-coalescing), and `??=` (null-coalescing assignment) to reduce boilerplate.
- **The "Bang" Operator (`!`):** Use sparingly. It tells the compiler "I know more than you," which is a common source of `NullReferenceException` if assumptions change.

## 3. Collections & Memory
- **Deferred Execution:** `IEnumerable` and LINQ use deferred execution. Be careful with multiple enumerations; use `.ToList()` or `.ToArray()` if you need to persist the result.
- **Never Return Null Collections:** Always return `Array.Empty<T>()`, `Enumerable.Empty<T>()`, or `[]` (collection expressions). It prevents `foreach` from crashing.
- **List vs. Array vs. Span:**
    - Use `List<T>` for dynamic growth.
    - Use `Array` for fixed-size sets.
    - Use `Span<T>` or `ReadOnlySpan<T>` for high-performance, allocation-free slicing of memory/strings.

## 4. Strings & Logging
- **StringComparison:** Never use `==` for business-logic string comparison. Always use `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)` or similar.
- **StringBuilder:** Use when concatenating strings in a loop. For simple 2-3 part joins, `string.Concat` or interpolation `$"..."` is often faster due to compiler optimizations.
- **Structured Logging:** Never embed variables directly in log strings: `Log("User {Id} failed", id)` NOT `Log($"User {id} failed")`. This allows log providers to index the `Id` property.

## 5. SOLID Principles in C#
- **S (Single Responsibility):** A `UserService` should manage users, not send emails. Delegate email to an `IEmailService`.
- **O (Open/Closed):** Use interfaces or abstract classes. Add new behavior by creating new implementations, not by modifying existing `switch` statements.
- **L (Liskov Substitution):** A `ReadOnlyFile` should not inherit from `File` if `File` has a `Write()` method that throws `NotImplementedException`.
- **I (Interface Segregation):** Prefer many small interfaces (`IReader`, `IWriter`) over one large `IFileHandler`.
- **D (Dependency Inversion):** High-level modules should not depend on low-level modules. Both should depend on abstractions (DI).

## 6. Records, Classes, and Structs
- **Classes:** Default choice for behavior-rich objects with identity and long lifetimes.
- **Records:** Use for data-centric objects (DTOs, Events). Provides built-in value equality and `with` expressions for non-destructive mutation.
- **Structs:** Use for small, short-lived data (e.g., a `Point` or `Color`) where the size is < 16 bytes. Avoid if they contain many reference types, as copying becomes expensive.

## 7. Exception Handling
- **What to Catch:** Only catch exceptions you can actually handle (e.g., retrying a network call). Never `catch (Exception)`.
- **Rethrowing:** 
    - Use `throw;` to preserve the stack trace. 
    - Never use `throw ex;` (it resets the stack trace).
    - Use `ExceptionDispatchInfo.Capture(ex).Throw()` if you need to rethrow from a different context (rare in modern `async/await`).
- **Custom Exceptions:** Create them only if you expect the caller to handle that specific failure differently from a standard exception.

## 8. IDisposable / IAsyncDisposable
- **The Pattern:** If a class owns an `IDisposable` field, it must also implement `IDisposable`.
- **Using Pattern:** Always use `using var ...` or `using (...) { ... }` to ensure disposal even if exceptions occur.
- **GC.SuppressFinalize:** Always call this in your `Dispose` method if your class has a finalizer (though you should rarely need a finalizer).
- **IAsyncDisposable:** Use for resources that require async cleanup (e.g., closing a network stream or a database connection).
