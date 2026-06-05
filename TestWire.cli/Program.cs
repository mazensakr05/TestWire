using System.CommandLine;
using TestWire.cli.Commands;

if (!Microsoft.Build.Locator.MSBuildLocator.IsRegistered)
	Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();


var rootCommand = new RootCommand("TestWire - Auto-generate integration test stubs for ASP.NET Core controllers");

var generateCommand = new GenerateCommand();
rootCommand.AddCommand(generateCommand);

return await rootCommand.InvokeAsync(args);