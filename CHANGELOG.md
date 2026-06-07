# Changelog

All notable changes to TestWire are documented here.

## [0.2.0] - 2026-06-07

### Added
- **NUnit Support**: Full support for generating NUnit test files and project structures.
- **Integration Testing**: Generated tests now use `WebApplicationFactory<Program>` by default for end-to-end integration testing.
- **Project File Generation**: The tool now automatically generates a `.csproj` for the test project with all required package references (xUnit/NUnit, Mvc.Testing, etc.).
- **Complex Type Support**: Improved analysis and generation for nested DTOs, collections, and complex generic return types.
- **Query & Header Support**: `[FromQuery]` and `[FromHeader]` parameters are now analyzed and integrated into generated test routes and requests.
- **Enhanced Sample API**: A more comprehensive sample API with multiple controllers and complex data structures for demonstration.

### Fixed
- **Stability**: Stabilized MSBuild analysis by downgrading to Roslyn 4.8.0 and improving SDK discovery.
- **Code Quality**: Fixed multiple formatting issues and unused `using` statements in generated output.
- **Namespace Resolution**: Automatically includes `.Models` and `.DTOs` namespaces in test files.
- **Method Naming**: Sanitized test method names for complex generic return types using regex.

### Changed
- **CLI Output**: Improved directory detection for the `--output` flag.
- **CI/CD**: GitHub Actions now target .NET 9.0.

## [0.1.2] - 2026-05-31

### Fixed
- Generated `using System.Threading;` directive now emits without stray leading whitespace
- `CancellationToken` parameters are now detected via Roslyn semantic model instead of fragile string matching — eliminates false positives on user-defined types ending with `CancellationToken`
- `CancellationToken` parameters now correctly receive `CancellationToken.None` as the generated argument value
- `ICollection<T>` and `IEnumerable<T>` return types now matched correctly (case-insensitive) in happy-path value generation
- `MSBuildWorkspace` is now disposed as a local `using` variable instead of a static field — fixes resource leak on repeated runs
- Attribute matching for `[Authorize]`, `[FromBody]`, and `[FromRoute]` now handles fully qualified attribute names (e.g. `AuthorizeAttribute`)
- `GetSemanticModel` call moved above the loop in `ResolveDependencyCalls` — eliminates redundant Roslyn workspace reloads per endpoint
- Fixed `using Moq;` conditional append using `Environment.NewLine` for cross-platform compatibility

## [0.1.0] - 2026-05-27

### Added
- Initial release
- ASP.NET Core controller test generation via Roslyn MSBuildWorkspace
- xUnit + Moq support
- Happy path, bad path, and security (`[Authorize]`) test generation
- Mocked constructor dependencies — every injected interface becomes `Mock<T>`
- DTO stubs with real property values generated from actual models
- `--dry-run` flag to preview output without writing files
- `--framework` flag to switch between xUnit and NUnit
- `--output` flag to specify output directory
