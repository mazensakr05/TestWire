using System.CommandLine;
using TestWire.cli.Analysis;
using System.CommandLine.Invocation;
namespace TestWire.cli.Commands;

public class GenerateCommand : Command
{
    public GenerateCommand() : base("generate", "Scan a .csproj and generate test stubs")
    {
        var projectOption = new Option<FileInfo>(
                name: "--project",
                description: "Path to the .csproj file to analyze")
            { IsRequired = true };

        var frameworkOption = new Option<string>(
            name: "--framework",
            description: "Test framework to use: xunit or nunit",
            getDefaultValue: () => "xunit");

        var outputOption = new Option<DirectoryInfo?>(
            name: "--output",
            description: "Directory to write generated test files");

        var dryRunOption = new Option<bool>(
            name: "--dry-run",
            description: "Print output to console without writing files");

        var reportOption = new Option<bool>(
            name: "--report",
            description: "Generate a testwire-report.md summary file");

        AddOption(projectOption);
        AddOption(frameworkOption);
        AddOption(outputOption);
        AddOption(dryRunOption);
        AddOption(reportOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            var project = context.ParseResult.GetValueForOption(projectOption)!;
            var framework = context.ParseResult.GetValueForOption(frameworkOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var report = context.ParseResult.GetValueForOption(reportOption);

            Console.WriteLine($"Analyzing {project.FullName}...");

            var controllers = await ProjectAnalyzer.AnalyzeAsync(project.FullName);

            if (controllers.Count == 0)
            {
                Console.WriteLine("No controllers found.");
                return;
            }

            foreach (var controller in controllers)
            {
                Console.WriteLine($"\n Controller: {controller.ClassName}");
                Console.WriteLine($"  Namespace: {controller.Namespace}");
                Console.WriteLine($"  BaseRoute: {controller.BaseRoute}");

                foreach (var endpoint in controller.Endpoints)
                {
                    Console.WriteLine($"    [{endpoint.HttpVerb}] {endpoint.MethodName} → {endpoint.Route}");
                }
            }
        });
    }
}