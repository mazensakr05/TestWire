# TestWire - Project Context

TestWire is a .NET-based CLI tool designed to autonomously generate test stubs for ASP.NET Core controllers. It uses Roslyn for semantic analysis of source code to understand controller dependencies, endpoints, and DTO structures, producing tailored test files that reduce boilerplate for developers.

## Project Overview

- **Main Technologies:** .NET 8, C#, Roslyn (Microsoft.CodeAnalysis), MSBuild Workspace, xUnit, Moq.
- **Architecture:**
  - **TestWire.cli:** The core CLI application.
    - `Analysis/`: Contains logic for scanning projects and extracting metadata from controllers using Roslyn.
    - `Generation/`: Handles the creation of test files based on the analyzed metadata.
    - `Commands/`: Implements CLI commands using `System.CommandLine`.
  - **SampleApi:** A reference ASP.NET Core Web API used for testing and demonstration.
  - **SampleApi.Tests:** Test suite for the SampleApi.

## Building and Running

### Prerequisites
- .NET 8 SDK or higher.

### Key Commands

- **Build Solution:**
  ```bash
  dotnet build
  ```

- **Run Tests:**
  ```bash
  dotnet test
  ```

- **Run TestWire CLI Locally:**
  ```bash
  dotnet run --project TestWire.cli/TestWire.cli.csproj -- generate --project ./SampleApi/SampleApi.csproj
  ```

- **Install as Global Tool (Local):**
  ```bash
  dotnet pack TestWire.cli/TestWire.cli.csproj
  dotnet tool install -g --add-source ./TestWire.cli/bin/Debug testwire
  ```

## Development Conventions

- **Code Analysis:** The project relies heavily on the `Microsoft.CodeAnalysis` (Roslyn) libraries. When modifying analysis logic, ensure `MSBuildLocator` is correctly initialized.
- **Testing Strategy:**
  - The tool generates both integration tests (using `WebApplicationFactory`) and unit tests (using `Moq`), depending on the generator configuration.
  - New features should be verified against the `SampleApi` to ensure generated code compiles and behaves as expected.
- **Coding Style:**
  - Standard C# naming conventions (PascalCase for classes/methods).
  - Use of primary constructors and modern C# features where appropriate.
  - Async/Await for I/O and analysis tasks.

## Key Files

- `TestWire.cli/Analysis/ProjectAnalyzer.cs`: The "brain" of the tool; performs the semantic scan of the target project.
- `TestWire.cli/Generation/TestFileGenerator.cs`: The primary engine for producing test code strings.
- `TestWire.cli/Commands/GenerateCommand.cs`: Defines the CLI interface and orchestrates the analysis-to-generation flow.
- `SampleApi/Controllers/ProductsController.cs`: A reference controller used to validate the tool's output.
