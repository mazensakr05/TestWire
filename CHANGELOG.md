# Changelog

All notable changes to TestWire are documented here.

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
