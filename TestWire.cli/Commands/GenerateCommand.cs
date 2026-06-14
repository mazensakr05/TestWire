using System.CommandLine;
using System.CommandLine.Invocation;
using System.Xml.Linq;
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
            
        var frameworkOption = new Option<TestFramework>(
            name: "--framework",
            description: "Test framework to use (XUnit or NUnit)",
            getDefaultValue: () => TestFramework.XUnit);

        var overwriteOption = new Option<bool>(
            name: "--overwrite",
            description: "Overwrite existing test files");

        var reportOption = new Option<bool>(
            name: "--report",
            description: "Generate a testwire-report.md summary file");

        AddOption(projectOption);
        AddOption(frameworkOption);
        AddOption(outputOption);
        AddOption(dryRunOption);
<<<<<<< Updated upstream
        AddOption(reportOption);
=======
        AddOption(frameworkOption);
        AddOption(overwriteOption);
>>>>>>> Stashed changes

        this.SetHandler(async (InvocationContext cliContext) =>
        {
<<<<<<< Updated upstream
            var project = context.ParseResult.GetValueForOption(projectOption)!;
            var framework = context.ParseResult.GetValueForOption(frameworkOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var report = context.ParseResult.GetValueForOption(reportOption);
=======
            var project = cliContext.ParseResult.GetValueForOption(projectOption)!;
            var output = cliContext.ParseResult.GetValueForOption(outputOption);
            var dryRun = cliContext.ParseResult.GetValueForOption(dryRunOption);
            var framework = cliContext.ParseResult.GetValueForOption(frameworkOption);
            var overwrite = cliContext.ParseResult.GetValueForOption(overwriteOption);
>>>>>>> Stashed changes

            Console.WriteLine($"Analyzing {project.FullName}...");

            var controllers = await ProjectAnalyzer.AnalyzeAsync(project.FullName);

            if (controllers.Count == 0)
            {
                Console.WriteLine("No controllers found.");
                return;
            }
            
            // Derive root namespace from first controller
            var projectNamespace = controllers[0].ProjectNamespace;
            var targetFramework = DetectTargetFramework(project.FullName);
            
            var genContext = new GenerationContext(
                ProjectNamespace: projectNamespace,
                TargetFramework: targetFramework,
                Framework: framework,
                OverwriteExisting: overwrite
            );

            if (dryRun)
            {
                foreach (var controller in controllers)
                {
<<<<<<< Updated upstream
                    var content = TestFileGenerator.Generate(controller, framework);
=======
                    var content = TestFileGenerator.Generate(controller, genContext);
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
                var outputDir = output?.FullName ?? Path.GetFullPath(Path.Combine(project.DirectoryName!, "..", $"{projectName}.Tests"));
                TestProjectGenerator.Generate(project.FullName, outputDir);

                foreach (var controller in controllers)
                {
                    var content = TestFileGenerator.Generate(controller, framework);
=======
                var rawOutput = cliContext.ParseResult.GetValueForOption(outputOption);

                var outputDir = rawOutput != null
                    ? (Directory.Exists(rawOutput) || !Path.HasExtension(rawOutput)
                        ? Path.GetFullPath(rawOutput)
                        : Path.GetDirectoryName(Path.GetFullPath(rawOutput))!)
                    : Path.GetFullPath(Path.Combine(project.DirectoryName!, "..", $"{projectName}.Tests"));

                // Generate .csproj + TestAuthHandler.cs + CustomWebApplicationFactory.cs
                TestProjectGenerator.Generate(project.FullName, outputDir, genContext);

                foreach (var controller in controllers)
                {
                    var content = TestFileGenerator.Generate(controller, genContext);
>>>>>>> Stashed changes
                    var fileName = $"{controller.ClassName}Tests.cs";
                    var filePath = Path.Combine(outputDir, fileName);

                    if (!File.Exists(filePath) || genContext.OverwriteExisting)
                    {
                        TestFileWriter.Write(filePath, content);
                        Console.WriteLine($"  ✅ Written → {filePath}");
                    }
                    else
                    {
                        Console.WriteLine($"  ⏭️ Skipped (already exists) → {filePath}");
                    }

                    foreach (var endpoint in controller.Endpoints)
                    {
                        Console.WriteLine($"    [{endpoint.HttpVerb}] {endpoint.MethodName} → {endpoint.Route}");
                    }
                }
            }
        });
    }

    private static string DetectTargetFramework(string csprojPath)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            var tf = doc.Descendants("TargetFramework").FirstOrDefault()?.Value;

            if (!string.IsNullOrWhiteSpace(tf)) return tf;
        }
        catch { }

        return "net8.0";
    }
}