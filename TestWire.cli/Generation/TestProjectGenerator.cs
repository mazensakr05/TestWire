using System.Xml.Linq;
namespace TestWire.cli.Generation;

public static class TestProjectGenerator
{
<<<<<<< Updated upstream
    public static void Generate(string targetCsprojPath, string outputDir)
    {
=======
    public static void Generate(string targetCsprojPath, string outputDir, GenerationContext context)
    {
        var resolvedDir = File.Exists(outputDir) || outputDir.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || outputDir.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            ? Path.GetDirectoryName(outputDir)!
            : outputDir;

>>>>>>> Stashed changes
        var projectName = Path.GetFileNameWithoutExtension(targetCsprojPath);
        var testCsprojPath = Path.Combine(outputDir, $"{projectName}.Tests.csproj");

        if (File.Exists(testCsprojPath)) return;

<<<<<<< Updated upstream
        var targetFramework = DetectTargetFramework(targetCsprojPath);
        var relativePath = Path.GetRelativePath(outputDir, targetCsprojPath);
=======
        var relativePath = Path.GetRelativePath(resolvedDir, targetCsprojPath);
>>>>>>> Stashed changes

        // Align ASP.NET testing package with the detected framework
        var mvcTestingVersion = context.TargetFramework.Contains("9.0") ? "9.0.0"
                              : context.TargetFramework.Contains("8.0") ? "8.0.0"
                              : context.TargetFramework.Contains("7.0") ? "7.0.0"
                              : "8.0.0"; // safe default

        var testingPackages = context.Framework == TestFramework.NUnit
            ? """
<PackageReference Include="NUnit" Version="4.2.2" />
<PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
"""
            : """
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
""";

        var content = $"""
<<<<<<< Updated upstream
                       <Project Sdk="Microsoft.NET.Sdk">

                         <PropertyGroup>
                           <TargetFramework>{targetFramework}</TargetFramework>
                           <IsPackable>false</IsPackable>
                           <Nullable>enable</Nullable>
                           <ImplicitUsings>enable</ImplicitUsings>
                         </PropertyGroup>

                         <ItemGroup>
                           <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
                           <PackageReference Include="xunit" Version="2.9.0" />
                           <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
                           <PackageReference Include="Moq" Version="4.20.70" />
                         </ItemGroup>

                         <ItemGroup>
                           <ProjectReference Include="{relativePath}" />
                         </ItemGroup>

                       </Project>
                       """;

        Directory.CreateDirectory(outputDir);
        File.WriteAllText(testCsprojPath, content);
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
=======
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>{context.TargetFramework}</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
{testingPackages}
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="{mvcTestingVersion}" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="{relativePath}" />
  </ItemGroup>

</Project>
""";

        Directory.CreateDirectory(resolvedDir);

        // Write the .csproj if it doesn't exist
        if (!File.Exists(testCsprojPath) || context.OverwriteExisting)
        {
            File.WriteAllText(testCsprojPath, content);
        }

        // Write auth scaffold files - these are infrastructure files every
        // generated test project needs to compile and run against [Authorize] endpoints
        var authHandlerPath = Path.Combine(resolvedDir, "TestAuthHandler.cs");
        var factoryPath = Path.Combine(resolvedDir, "CustomWebApplicationFactory.cs");

        // Write these if they are missing or if we want to ensure latest version
        if (!File.Exists(authHandlerPath) || context.OverwriteExisting)
            TestFileWriter.Write(authHandlerPath, AuthScaffoldGenerator.GenerateTestAuthHandler(context.ProjectNamespace));

        if (!File.Exists(factoryPath) || context.OverwriteExisting)
            TestFileWriter.Write(factoryPath, AuthScaffoldGenerator.GenerateCustomWebApplicationFactory(context.ProjectNamespace));
>>>>>>> Stashed changes
    }

}