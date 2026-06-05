using System.CommandLine;
using System.CommandLine.Invocation;
using TestWire.cli.Analysis;
using TestWire.cli.Generation;

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

            if (dryRun)
            {
                foreach (var controller in controllers)
                {
                    var content = TestFileGenerator.Generate(controller, framework);
                    Console.WriteLine("\n--- Generated Test File ---");
                    Console.WriteLine(content);

                    foreach (var endpoint in controller.Endpoints)
                    {
                        Console.WriteLine($"    [{endpoint.HttpVerb}] {endpoint.MethodName} → {endpoint.Route}");
                    }
                }
            }
            else
            {
                var projectName = Path.GetFileNameWithoutExtension(project.FullName);
                var outputDir = output?.FullName ?? Path.GetFullPath(Path.Combine(project.DirectoryName!, "..", $"{projectName}.Tests"));
                TestProjectGenerator.Generate(project.FullName, outputDir);

                foreach (var controller in controllers)
                {
                    var content = TestFileGenerator.Generate(controller, framework);
                    var fileName = $"{controller.ClassName}Tests.cs";
                    var filePath = Path.Combine(outputDir, fileName);

                    TestFileWriter.Write(filePath, content);
                    Console.WriteLine($"  ✅ Written → {filePath}");

                    foreach (var endpoint in controller.Endpoints)
                    {
                        Console.WriteLine($"    [{endpoint.HttpVerb}] {endpoint.MethodName} → {endpoint.Route}");
                    }
                }
            }
        });
    }
}