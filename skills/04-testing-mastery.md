# Testing Mastery: Senior Engineer Reference

Timeless principles for writing reliable, maintainable, and fast tests in the .NET ecosystem.

## 1. xUnit vs. NUnit
- **xUnit:** The modern standard. No `[TestFixture]` needed. Constructor is for setup, `Dispose` is for teardown. Parallel execution by default.
- **NUnit:** The classic choice. Uses `[SetUp]` and `[TearDown]`. Explicit parallelization configuration.
- **Recommendation:** Use xUnit for new projects unless NUnit features (like `[TestCaseSource]` flexibility) are specifically required.

## 2. The AAA Pattern
Every test should follow these three steps:
1. **Arrange:** Set up the system under test (SUT) and its dependencies.
2. **Act:** Execute the specific behavior being tested.
3. **Assert:** Verify that the result matches expectations.
- **Rule:** Keep the "Act" line as small as possible (ideally one line).

## 3. Mocking & Dependencies
- **Mocking (NSubstitute/Moq):** Use mocks to isolate the SUT from external dependencies (DBs, APIs, File System).
- **Stubbing:** Providing canned answers to calls (`Substitute.For<IService>().Get().Returns(data)`).
- **Verification:** Checking that a call happened (`service.Received().Save(...)`). Use sparingly; over-testing interactions leads to brittle tests.
- **Fakes:** Manual implementations (e.g., `InMemoryRepository`) are often better than complex mocks for deeply nested logic.

## 4. Assertions
- **FluentAssertions:** Prefer `result.Should().Be(expected)` over `Assert.Equal(expected, result)`. It produces much more readable error messages.
- **Collection Assertions:** `list.Should().HaveCount(3).And.Contain(item)`.
- **Exception Assertions:** `action.Should().Throw<InvalidOperationException>().WithMessage("...");`.

## 5. Test Isolation & Reliability
- **State Leakage:** Tests must NEVER depend on each other. Each test should be able to run in any order or in parallel.
- **Time Sensitivity:** Don't use `DateTime.Now`. Inject an `IDateTimeProvider` so you can control time in your tests.
- **Flaky Tests:** Identify and fix flaky tests immediately. They destroy trust in the CI pipeline.

## 6. Integration Testing (ASP.NET Core)
- **WebApplicationFactory:** Use it to test the entire stack from HTTP request to response.
- **Real DB vs. In-Memory:** Use a real database (e.g., via Testcontainers) for integration tests to catch schema or constraint issues that In-Memory providers miss.
- **Slice Testing:** Test a vertical slice of the application (e.g., Controller -> Service -> DB) rather than just isolated units.

## 7. Data-Driven Tests
- **xUnit [Theory]:** Use `[InlineData]` for simple values or `[MemberData]` / `[ClassData]` for complex objects.
- **Avoid Logic in Tests:** If you need `if` statements or loops in your test, it's a sign that the test is too complex. Split it into multiple simpler tests.

## 8. Naming Conventions
- **Standard:** `MethodName_StateUnderTest_ExpectedBehavior` (e.g., `Withdraw_AmountGreaterThanBalance_ThrowsException`).
- **Goal:** A failing test name should tell you exactly what went wrong without reading the code.
