using System.CommandLine;
using TestWire.cli.Commands;

var rootCommand = new RootCommand("TestWire - Auto-generate integration test stubs for ASP.NET Core controllers");

var generateCommand = new GenerateCommand();
rootCommand.AddCommand(generateCommand);

return await rootCommand.InvokeAsync(args);