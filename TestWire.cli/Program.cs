using System.CommandLine;
using TestWire.cli.Commands;

try
{
	if (!Microsoft.Build.Locator.MSBuildLocator.IsRegistered)
		Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();
}
catch (Exception ex)
{
	Console.ForegroundColor = ConsoleColor.Red;
	Console.Error.WriteLine($"[TestWire] Failed to initialize MSBuild: {ex.Message}");
	Console.Error.WriteLine("Make sure the .NET SDK is installed and accessible.");
	Console.ResetColor();
	return 1;
}

var rootCommand = new RootCommand("TestWire - Auto-generate integration test stubs for ASP.NET Core controllers");

var generateCommand = new GenerateCommand();
rootCommand.AddCommand(generateCommand);

return await rootCommand.InvokeAsync(args);