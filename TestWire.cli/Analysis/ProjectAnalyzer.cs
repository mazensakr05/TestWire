using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;


namespace TestWire.cli.Analysis;

public class ProjectAnalyzer
{
    public static async Task<List<ControllerInfo>> AnalyzeAsync(string csprojPath)
    {
        var controllers = new List<ControllerInfo>();

        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(csprojPath);
        var compilation = await project.GetCompilationAsync();

        if (compilation == null)
            throw new Exception("Failed to compile project.");

        foreach (var document in project.Documents)
        {
            // skip files in OBJ folder
            if (document.FilePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") == true)
                continue;

            var syntexTree = await document.GetSyntaxTreeAsync();
            if (syntexTree == null) continue;
            var root = await syntexTree.GetRootAsync();
            var semanticModel = compilation.GetSemanticModel(syntexTree);

            var classDeclarations = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => c.Identifier.Text.EndsWith("Controller"));

            foreach (var classDecl in classDeclarations)
            {
                var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                if (classSymbol == null) continue;

                var controllerInfo = new ControllerInfo
                {
                    ClassName = classSymbol.Name,
                    Namespace = classSymbol.ContainingNamespace?.ToDisplayString(),
                    BaseRoute = GetAttributeArgument(classSymbol, "Route") ?? string.Empty,
                    Dependencies = GetConstructorDependencies(classSymbol),
                };

                foreach (var member in classSymbol.GetMembers().OfType<IMethodSymbol>())
                {
                    var verb = GetHttpVerb(member);
                    if (verb is null) continue;

                   var unwrapped = UnwrapReturnType(member.ReturnType);

                    // layer 2 - producesResponseType Attribute

                    if (string.IsNullOrEmpty(unwrapped)){
                        unwrapped = GetProducesResponseDetails(member)
                            .FirstOrDefault(r => r.StatusCode == 200)?.TypeName
                            ?? string.Empty;
                    }

                    // layer 3 - try to infer from return statements in method body
                    if (string.IsNullOrEmpty(unwrapped))
                    {
                        unwrapped = await TryInferReturnTypeFromBody(member, semanticModel) ?? string.Empty;
                    }
                    var endpointInfo = new EndpointInfo
                    {
                        MethodName = member.Name,
                        HttpVerb = verb,
                        Route = GetAttributeArgument(member, verb)
                        ?? GetAttributeArgument(member, "Route")
                        ?? string.Empty,
                        IsAsync = member.IsAsync,
                        HasAllowAnonymous = HasAttribute(member, "AllowAnonymous"),
                        HasAuthorize = (HasAttribute(member, "Authorize") || HasAttribute(classSymbol, "Authorize"))
                        && !HasAttribute(member, "AllowAnonymous"),
                        ReturnType = unwrapped,
                        ProducesResponses = GetProducesResponseDetails(member),
                        Parameters = new List<ParameterDetail>()
                    };

                    foreach (var param in member.Parameters)
                    {
                        var paramDetail = new ParameterDetail
                        {
                            Name = param.Name,
                            Type = param.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                            IsFromBody = HasAttribute(param, "FromBody"),
                            IsFromRoute = HasAttribute(param, "FromRoute"),
                            IsFromQuery = HasAttribute(param, "FromQuery")
                        };

                        if (param.Type is INamedTypeSymbol paramTypeSymbol
                            && IsComplexUserType(param.Type))
                        {
                            paramDetail.DtoProperties = ReadDtoProperties(paramTypeSymbol);
                        }

                        endpointInfo.Parameters.Add(paramDetail);
                    }

                    controllerInfo.Endpoints.Add(endpointInfo);
                }
                controllers.Add(controllerInfo);


            }
        }

        return controllers;

    }



    public static List<PropertyDetail> ReadDtoProperties(INamedTypeSymbol typeSymbol)
    {
        var list = new List<PropertyDetail>();

        foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.DeclaredAccessibility == Accessibility.Public && !member.IsStatic)
            {
                list.Add(new PropertyDetail
                {
                    Name = member.Name,
                    Type = member.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                });
            }
        }
        return list;

    }



    private static List<ConstructorDependency> GetConstructorDependencies(INamedTypeSymbol classSymbol)
    {
        var constructors = classSymbol.Constructors;

        if (constructors.Length == 0) return new List<ConstructorDependency>();

        var primary = constructors.OrderByDescending(c => c.Parameters.Length).First();
        var dependencies = new List<ConstructorDependency>();
        foreach (var parameter in primary.Parameters)
        {


            dependencies.Add(new ConstructorDependency
            {
                Name = parameter.Name,
                Type = parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            });

        }
        return dependencies;
    }

    private static string UnwrapReturnType(ITypeSymbol typeSymbol)
    {
        string[] names = ["Task", "ActionResult", "IActionResult"];
        // Cast attempt
        if (typeSymbol is not INamedTypeSymbol namedType)
            return typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        // Check for Task<T>
        if (names.Contains(namedType.Name))
        {

            if (namedType.TypeArguments.Length > 0)
            {

                return UnwrapReturnType(namedType.TypeArguments[0]);


            }
            else return string.Empty;


        }
        return namedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);


    }

    private static bool HasAttribute(ISymbol symbol, string attributeName)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            var name = attr.AttributeClass?.Name;
            if (name == null) continue;
            var cleanName = name.EndsWith("Attribute") ? name.Substring(0, name.Length - 9) : name;

            if (cleanName == attributeName)
                return true;
        }
        return false;
    }

    private static string? GetAttributeArgument(ISymbol symbol, string attributeName)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            var name = attr.AttributeClass?.Name;
            if (name == null) continue;
            var cleanName = name.EndsWith("Attribute") ? name.Substring(0, name.Length - 9) : name;
            if (cleanName == attributeName && attr.ConstructorArguments.Length > 0)
                return attr.ConstructorArguments[0].Value?.ToString();

        }
        return null;
    }

    private static string? GetHttpVerb(IMethodSymbol methodSymbol)
    {
        string[] verbs = ["HttpGet", "HttpPost", "HttpPut", "HttpDelete", "HttpPatch"];
        foreach (var verb in verbs)
        {
            if (HasAttribute(methodSymbol, verb))
                return verb;
        }
        return null;
    }

    private static bool IsComplexUserType(ITypeSymbol typeSymbol)
    {
        // condition 1 : if its a primitive STOP ! 

        if (typeSymbol.SpecialType != SpecialType.None)
        {
            return false;

        }
        // Condition 2  if it's a System/Microsoft type, stop
        var ns = typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        if (ns.StartsWith("System") || ns.StartsWith("Microsoft"))
            return false;

        // Condition 3  must be a class or struct
        return typeSymbol.TypeKind == TypeKind.Class ||
               typeSymbol.TypeKind == TypeKind.Struct;

    }

    private static List<ProducesResponseDetail> GetProducesResponseDetails(IMethodSymbol methodSymbol)
    {
        var result = new List<ProducesResponseDetail>();

        foreach (var attr in methodSymbol.GetAttributes())
        {

            var name = attr.AttributeClass?.Name;
            if (name == null) continue;

            var cleanName = name.EndsWith("Attribute") ? name.Substring(0, name.Length - 9) : name;

            if (cleanName != "ProducesResponseType") continue;


            if (attr.ConstructorArguments.Length == 0) continue;

            var detail = new ProducesResponseDetail();

            // Case 1: [ProducesResponseType(typeof(T), statusCode)]
            if (attr.ConstructorArguments[0].Kind == TypedConstantKind.Type)
            {
                var typeSymbol = attr.ConstructorArguments[0].Value as ITypeSymbol;
                detail.TypeName = typeSymbol?
                    .ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                // status code is the second argument
                if (attr.ConstructorArguments.Length > 1)
                    detail.StatusCode = (int)(attr.ConstructorArguments[1].Value ?? 200);
            }
            // Case 2: [ProducesResponseType(statusCode)]
            else if (attr.ConstructorArguments[0].Kind == TypedConstantKind.Primitive)
            {
                detail.StatusCode = (int)(attr.ConstructorArguments[0].Value ?? 200);
                detail.TypeName = null; // no type declared
            }

            result.Add(detail);
        }
        return result;
    }


    private static async Task<string?> TryInferReturnTypeFromBody(IMethodSymbol methodSymbol, SemanticModel semanticModel)
    {
        var syntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxRef == null) return null;
        var methodSyntax = await syntaxRef.GetSyntaxAsync() as MethodDeclarationSyntax;
        if (methodSyntax?.Body is null) return null;

        var resultMethods = new HashSet<string> { "Ok", "Created", "CreatedAtAction", "CreatedAtRoute", "Accepted", "AcceptedAtAction" };

        var returnStatements = methodSyntax.Body.DescendantNodes().OfType<ReturnStatementSyntax>();

        foreach (var returnStatement in returnStatements)
        {
            if (returnStatement.Expression is not InvocationExpressionSyntax invocation) continue;
            
            var methodName =  invocation.Expression is MemberAccessExpressionSyntax memberAccess ? memberAccess.Name.Identifier.Text : (invocation.Expression as IdentifierNameSyntax)?.Identifier.Text;

            if (methodName is null || !resultMethods.Contains(methodName)) continue;

            var arg = invocation.ArgumentList.Arguments.FirstOrDefault();
            if(arg == null) continue;

            var typeInfo = semanticModel.GetTypeInfo(arg.Expression);
            if(typeInfo.Type is null ) continue;

            return typeInfo.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        }

        return null;
    }


}