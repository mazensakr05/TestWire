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
                var dependencies = GetConstructorDependencies(classDecl); 
                var controllerInfo = new ControllerInfo
                {
                    ClassName = classDecl.Identifier.Text,
                    Namespace = GetNamespace(classDecl),
                    BaseRoute = GetAttributeArgument(classDecl.AttributeLists, "Route") ?? string.Empty,
                    Dependencies = dependencies 

                };

                foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
                {
                    var verb = GetHttpVerb(method.AttributeLists);
                    if (verb is null) continue;

                    var returnType = method.ReturnType.ToString();
                    var isAsync = returnType.StartsWith("Task");
                    var cleanReturn = returnType
                        .Replace("Task<", "")
                        .Replace("ActionResult<", "")
                        .Replace("IActionResult", "")
                        .TrimEnd('>');

                    var hasAuthorize = method.AttributeLists
                                           .SelectMany(a => a.Attributes)
                                           .Any(a => a.Name.ToString().Contains("Authorize"))
                                       || classDecl.AttributeLists
                                           .SelectMany(a => a.Attributes)
                                           .Any(a => a.Name.ToString().Contains("Authorize"));

                    var parameters = new List<ParameterDetail>();

                    foreach (var p in method.ParameterList.Parameters)
                    {
                        var param = new ParameterDetail
                        {
                            Name = p.Identifier.Text,
                            Type = p.Type?.ToString() ?? "Object",
                            IsFromBody = p.AttributeLists.SelectMany(a => a.Attributes)
                                .Any(a => a.Name.ToString().Contains("FromBody")),
                            IsFromRoute = p.AttributeLists.SelectMany(a => a.Attributes)
                                .Any(a => a.Name.ToString().Contains("FromRoute")),
                        };

                        if (LooksLikeDto(param.Type))
                        {
                            var dtoClass = await FindDtoClassAsync(
                                param.Type, csFiles);
                            if (dtoClass != null)
                            {
                                param.DtoProperties = ReadDtoProperties(dtoClass);
                            }
                            
                        }
                        parameters.Add(param);
                    }

                    controllerInfo.Endpoints.Add(new EndpointInfo
                    {
                        MethodName = method.Identifier.Text,
                        HttpVerb = verb,
                        Route = GetAttributeArgument(method.AttributeLists, verb)
                                ?? GetAttributeArgument(method.AttributeLists, "Route")
                                ?? string.Empty,
                        ReturnType = cleanReturn.Trim(),
                        IsAsync = isAsync,
                        HasAuthorize = hasAuthorize,
                        Parameters = parameters,
                        DependencyCalls = GetDependencyCalls(method, dependencies)
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


    private static async Task<ClassDeclarationSyntax?> FindDtoClassAsync(string className, List<string> csFiles)
    {
        foreach (var file in csFiles)
        {
            var source =  await File.ReadAllTextAsync(file);
            var tree = CSharpSyntaxTree.ParseText(source);
            var root = await tree.GetRootAsync();
            
            var match  = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text == className);
            if (match != null) 
                return match;
        }
        
        return null;
    }

    public static List<PropertyDetail> ReadDtoProperties(ClassDeclarationSyntax dtoClass)
    {
        return dtoClass.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(p => p.Modifiers.Any(m => m.Text == "public"))
            .Select(p => new PropertyDetail
            {
                Name = p.Identifier.Text,
                Type = p.Type.ToString()
            }).ToList();
    }

    private static bool LooksLikeDto(string type)
    {
        var lower = type.ToLower();
        return lower.EndsWith("dto")
               || lower.EndsWith("command")
               || lower.EndsWith("model")
               || lower.EndsWith("input")
               || lower.EndsWith("payload");
    }

    private static List<ConstructorDependency> GetConstructorDependencies(ClassDeclarationSyntax classDecl)
    {
        var constructor = classDecl.Members
            .OfType<ConstructorDeclarationSyntax>()
            .OrderByDescending(c => c.ParameterList.Parameters.Count)
            .FirstOrDefault();

        if (constructor == null) return new List<ConstructorDependency>();

        return constructor.ParameterList.Parameters.Select(p => new ConstructorDependency
        {
            Type = p.Type?.ToString() ?? "object",
            Name = p.Identifier.Text
        }).ToList();
    }
    
    private static List<DependencyCallInfo> GetDependencyCalls(
        MethodDeclarationSyntax method,
        List<ConstructorDependency> dependencies)
    {
        var dependencyNames = dependencies
            .Select(d => d.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var calls = new List<DependencyCallInfo>();

        var invocations = method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                continue;

            var target = memberAccess.Expression.ToString().TrimStart('_');
            if (!dependencyNames.Contains(target))
                continue;

            var dependency = dependencies.First(d =>
                string.Equals(d.Name, target, StringComparison.OrdinalIgnoreCase));

            var methodName = memberAccess.Name.Identifier.Text;
            var argumentTypes = invocation.ArgumentList.Arguments
                .Select(a => a.Expression.ToString())
                .ToList();

            calls.Add(new DependencyCallInfo
            {
                DependencyName = dependency.Name,
                DependencyType = dependency.Type,
                MethodName = methodName,
                ArgumentTypes = argumentTypes,
                ReturnType = string.Empty,
                IsAsync = methodName.EndsWith("Async")
            });
        }

        return calls;
    }
}