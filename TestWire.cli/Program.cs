using System.CommandLine;
using TestWire.cli.Commands;

// MSBuild registration is intentionally deferred to ProjectAnalyzer.AnalyzeAsync().
// Registering here (before any MSBuild types are loaded) can race or conflict;
// the analyzer handles multi-strategy discovery that works across SDK-only machines,
// Visual Studio installations, and CI environments.

var rootCommand = new RootCommand("TestWire - Auto-generate integration test stubs for ASP.NET Core controllers");

var generateCommand = new GenerateCommand();
rootCommand.AddCommand(generateCommand);

return await rootCommand.InvokeAsync(args);