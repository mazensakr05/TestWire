using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace TestWire.cli.Analysis;

public class ProjectAnalyzer
{
    private static MSBuildWorkspace? _workspace;

    public static async Task<List<ControllerInfo>> AnalyzeAsync(string csprojPath)
    {
        _workspace = MSBuildWorkspace.Create();

        var project = await _workspace.OpenProjectAsync(csprojPath);
        var compilation = await project.GetCompilationAsync();
        if (compilation is null)
            throw new InvalidOperationException($"Failed to compile project: {csprojPath}");

        var typeLookup = BuildTypeLookup(compilation);
        var controllers = new List<ControllerInfo>();

        foreach (var document in project.Documents)
        {
            var semanticModel = await document.GetSemanticModelAsync();
            if (semanticModel is null) continue;

            var syntaxRoot = await document.GetSyntaxRootAsync();
            if (syntaxRoot is null) continue;

            var classDeclarations = syntaxRoot
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => c.Identifier.Text.EndsWith("Controller"));

            foreach (var classDecl in classDeclarations)
            {
                var dependencies = ResolveFieldToConstructorParam(classDecl, semanticModel);

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
                                               .Any(a => a.Name.ToString() == "Authorize")
                                           || classDecl.AttributeLists
                                               .SelectMany(a => a.Attributes)
                                               .Any(a => a.Name.ToString() == "Authorize");

                    var parameters = new List<ParameterDetail>();
                    foreach (var p in method.ParameterList.Parameters)
                    {
                        var param = new ParameterDetail
                        {
                            Name = p.Identifier.Text,
                            Type = p.Type?.ToString() ?? "Object",
                            IsFromBody = p.AttributeLists.SelectMany(a => a.Attributes)
                                .Any(a => a.Name.ToString() == "FromBody"),
                            IsFromRoute = p.AttributeLists.SelectMany(a => a.Attributes)
                                .Any(a => a.Name.ToString() == "FromRoute"),
                        };

                        if (LooksLikeDto(param.Type))
                        {
                            var dtoClass = FindDtoClassByName(param.Type, typeLookup);
                            if (dtoClass != null)
                            {
                                param.DtoProperties = ReadDtoProperties(dtoClass);
                            }
                        }
                        parameters.Add(param);
                    }

                    var dependencyCalls = ResolveDependencyCalls(
                        method, dependencies, compilation);

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
                        DependencyCalls = dependencyCalls
                    });
                }

                controllers.Add(controllerInfo);
            }
        }

        return controllers;
    }

    private static Dictionary<string, ClassDeclarationSyntax> BuildTypeLookup(
        CSharpCompilation compilation)
    {
        var lookup = new Dictionary<string, ClassDeclarationSyntax>();

        foreach (var tree in compilation.SyntaxTrees)
        {
            var root = tree.GetRoot();
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                lookup.TryAdd(classDecl.Identifier.Text, classDecl);
            }
            foreach (var structDecl in root.DescendantNodes().OfType<StructDeclarationSyntax>())
            {
                lookup.TryAdd(structDecl.Identifier.Text, structDecl);
            }
        }

        return lookup;
    }

    private static List<ConstructorDependency> ResolveFieldToConstructorParam(
        ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        var constructor = classDecl.Members
            .OfType<ConstructorDeclarationSyntax>()
            .OrderByDescending(c => c.ParameterList.Parameters.Count)
            .FirstOrDefault();

        if (constructor == null) return new List<ConstructorDependency>();

        var result = new List<ConstructorDependency>();

        foreach (var assignment in constructor.Body?.DescendantNodes()
                     .OfType<AssignmentExpressionSyntax>() ?? Enumerable.Empty<AssignmentExpressionSyntax>())
        {
            if (semanticModel.GetSymbolInfo(assignment.Left).Symbol is IFieldSymbol field &&
                semanticModel.GetSymbolInfo(assignment.Right).Symbol is IParameterSymbol param)
            {
                result.Add(new ConstructorDependency
                {
                    Type = field.Type.ToDisplayString(),
                    Name = param.Name
                });
            }
        }

        if (result.Count == 0)
        {
            return constructor.ParameterList.Parameters.Select(p => new ConstructorDependency
            {
                Type = p.Type?.ToString() ?? "object",
                Name = p.Identifier.Text
            }).ToList();
        }

        return result;
    }

    private static List<DependencyCallInfo> ResolveDependencyCalls(
        MethodDeclarationSyntax method,
        List<ConstructorDependency> dependencies,
        CSharpCompilation compilation)
    {
        var calls = new List<DependencyCallInfo>();
        var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                continue;

            var expressionSymbol = semanticModel: compilation
                .GetSemanticModel(invocation.SyntaxTree)
                .GetSymbolInfo(memberAccess.Expression).Symbol;

            if (expressionSymbol is null) continue;

            var typeName = expressionSymbol switch
            {
                IFieldSymbol field => field.Type.ToDisplayString(),
                IPropertySymbol prop => prop.Type.ToDisplayString(),
                IParameterSymbol param => param.Type.ToDisplayString(),
                _ => null
            };

            if (typeName is null) continue;

            var matchingDep = dependencies.FirstOrDefault(d =>
                string.Equals(d.Type, typeName, StringComparison.Ordinal));

            if (matchingDep is null) continue;

            var methodSymbol = compilation
                .GetSemanticModel(invocation.SyntaxTree)
                .GetSymbolInfo(invocation).Symbol as IMethodSymbol;

            if (methodSymbol is null) continue;

            var argExpressions = invocation.ArgumentList?.Arguments
                .Select(a => a.Expression.ToString()).ToList() ?? new List<string>();

            var argTypes = methodSymbol.Parameters
                .Select(p => p.Type.ToDisplayString()).ToList();

            var returnType = methodSymbol.ReturnType.ToDisplayString();
            var isAsync = false;

            if (methodSymbol.ReturnType is INamedTypeSymbol namedReturn &&
                namedReturn.IsGenericType &&
                namedReturn.ConstructedFrom.ToDisplayString() == "System.Threading.Tasks.Task<T>")
            {
                returnType = namedReturn.TypeArguments[0].ToDisplayString();
                isAsync = true;
            }
            else if (methodSymbol.ReturnType.ToDisplayString() == "System.Threading.Tasks.Task")
            {
                returnType = "void";
                isAsync = true;
            }

            calls.Add(new DependencyCallInfo
            {
                DependencyName = matchingDep.Name,
                DependencyType = matchingDep.Type,
                MethodName = methodSymbol.Name,
                ArgumentExpressions = argExpressions,
                ArgumentTypes = argTypes,
                ReturnType = returnType,
                IsAsync = isAsync
            });
        }

        return calls;
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

    private static ClassDeclarationSyntax? FindDtoClassByName(
        string className, Dictionary<string, ClassDeclarationSyntax> typeLookup)
    {
        return typeLookup.TryGetValue(className, out var classDecl) ? classDecl : null;
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
               || lower.EndsWith("payload")
               || lower.EndsWith("request")
               || lower.EndsWith("response")
               || lower.EndsWith("query")
               || lower.EndsWith("filter");
    }

    public static bool IsLoggerDependency(string typeName)
    {
        var outerType = typeName;
        var genericStart = outerType.IndexOf('<');
        if (genericStart >= 0)
        {
            outerType = outerType.Substring(0, genericStart);
        }

        var lastDot = outerType.LastIndexOf('.');
        var lastAliasSeparator = outerType.LastIndexOf("::");
        var separatorIndex = Math.Max(lastDot, lastAliasSeparator);
        var lastSegment = separatorIndex >= 0
            ? outerType.Substring(separatorIndex + (separatorIndex == lastAliasSeparator ? 2 : 1))
            : outerType;

        return lastSegment == "ILogger";
    }
}
