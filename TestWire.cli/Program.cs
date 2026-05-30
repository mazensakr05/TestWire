using System.CommandLine;
using Microsoft.Build.Locator;
using TestWire.cli.Commands;

MSBuildLocator.RegisterDefaults();

var rootCommand = new RootCommand("TestWire - Auto-generate integration test stubs for ASP.NET Core controllers");

var generateCommand = new GenerateCommand();
rootCommand.AddCommand(generateCommand);

return await rootCommand.InvokeAsync(args);
