using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace TestWire.cli.Analysis;

public class ProjectAnalyzer
{
    public static async Task<List<ControllerInfo>> AnalyzeAsync(string csprojPath)
    {
        var projectDir = Path.GetDirectoryName(csprojPath)!;
        var csFiles = GetCsFiles(csprojPath, projectDir);
        var controllers = new List<ControllerInfo>();

        foreach (var file in csFiles)
        {
            var source = await File.ReadAllTextAsync(file);
            var tree = CSharpSyntaxTree.ParseText(source);
            var root = await tree.GetRootAsync();

            var classDeclarations = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => c.Identifier.Text.EndsWith("Controller"));

            foreach (var classDecl in classDeclarations)
            {
                var controllerInfo = new ControllerInfo
                {
                    ClassName = classDecl.Identifier.Text,
                    Namespace = GetNamespace(classDecl),
                    BaseRoute = GetAttributeArgument(classDecl.AttributeLists, "Route") ?? string.Empty
                };

                foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
                {
                    var verb = GetHttpVerb(method.AttributeLists);
                    if (verb is null) continue;

                    controllerInfo.Endpoints.Add(new EndpointInfo
                    {
                        MethodName = method.Identifier.Text,
                        HttpVerb = verb,
                        Route = GetAttributeArgument(method.AttributeLists, verb)
                                ?? GetAttributeArgument(method.AttributeLists, "Route")
                                ?? string.Empty
                    });
                }

                controllers.Add(controllerInfo);
            }
        }

        return controllers;
    }

    private static List<string> GetCsFiles(string csprojPath, string projectDir)
    {
        var doc = XDocument.Load(csprojPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        // Check for explicit Compile includes
        var explicitFiles = doc.Descendants(ns + "Compile")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => v != null)
            .Select(v => Path.GetFullPath(Path.Combine(projectDir, v!)))
            .ToList();

        if (explicitFiles.Count > 0)
            return explicitFiles;

        // Default: grab all .cs files recursively (SDK-style projects)
        return Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToList();
    }

    private static string? GetHttpVerb(SyntaxList<AttributeListSyntax> attributeLists)
    {
        string[] verbs = ["HttpGet", "HttpPost", "HttpPut", "HttpDelete", "HttpPatch"];

        foreach (var attrList in attributeLists)
            foreach (var attr in attrList.Attributes)
                if (verbs.Contains(attr.Name.ToString()))
                    return attr.Name.ToString();

        return null;
    }

    private static string? GetAttributeArgument(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
    {
        foreach (var attrList in attributeLists)
            foreach (var attr in attrList.Attributes)
                if (attr.Name.ToString() == attributeName)
                    return attr.ArgumentList?.Arguments.FirstOrDefault()
                        ?.ToString().Trim('"');

        return null;
    }

    private static string GetNamespace(ClassDeclarationSyntax classDecl)
    {
        return classDecl.Ancestors()
                   .OfType<NamespaceDeclarationSyntax>()
                   .FirstOrDefault()?.Name.ToString()
               ?? classDecl.Ancestors()
                   .OfType<FileScopedNamespaceDeclarationSyntax>()
                   .FirstOrDefault()?.Name.ToString()
               ?? string.Empty;
    }
}