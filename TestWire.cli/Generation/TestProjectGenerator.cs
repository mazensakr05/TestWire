using System.Xml.Linq;
namespace TestWire.cli.Generation;

public class TestProjectGenerator
{
    public static void Generate(string targetCsprojPath, string outputDir)
    {
        var resolvedDir = File.Exists(outputDir) || Path.HasExtension(outputDir)
            ? Path.GetDirectoryName(outputDir)!
            : outputDir;

        var projectName = Path.GetFileNameWithoutExtension(targetCsprojPath);
        var testCsprojPath = Path.Combine(resolvedDir, $"{projectName}.Tests.csproj");

        if (File.Exists(testCsprojPath)) return;

        var targetFramework = DetectTargetFramework(targetCsprojPath);
        var relativePath = Path.GetRelativePath(resolvedDir, targetCsprojPath);

        var content = $"""
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
                       <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
                     </ItemGroup>

                     <ItemGroup>
                       <ProjectReference Include="{relativePath}" />
                     </ItemGroup>

                   </Project>
                   """;

        Directory.CreateDirectory(resolvedDir);
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
    }
}