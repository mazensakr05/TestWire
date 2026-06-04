using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;


namespace TestWire.cli.Analysis;

public class ProjectAnalyzer
{
   public static async Task<List<ControllerInfo>> AnalyzeAsync( string csprojPath)
    {
       var controllers = new List<ControllerInfo>();

        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(csprojPath);
        var compilation = await project.GetCompilationAsync();

        if(compilation == null)
            throw new Exception("Failed to compile project.");

        foreach(var document in project.Documents)
        {
            // skip files in OBJ folder
            if(document.FilePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") == true)
                continue;

            var syntexTree = await document.GetSyntaxTreeAsync();
            if(syntexTree == null) continue;
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
                        ReturnType = UnwrapReturnType(member.ReturnType),
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
        foreach ( var parameter in primary.Parameters) {
           
            
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
        string [] names  = ["Task", "ActionResult", "IActionResult"];
        // Cast attempt
        if (typeSymbol is not INamedTypeSymbol namedType)
            return typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        // Check for Task<T>
        if (names.Contains(namedType.Name)){

            if (namedType.TypeArguments.Length > 0) {

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
            if (name == null ) continue;
            var cleanName = name.EndsWith("Attribute") ? name.Substring(0, name.Length - 9) : name;
      
            if (cleanName == attributeName)
                return true;
        }
        return false;
    }

    private static string? GetAttributeArgument(ISymbol symbol , string attributeName)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            var name = attr.AttributeClass?.Name;
            if (name == null ) continue;
            var cleanName = name.EndsWith("Attribute") ? name.Substring(0, name.Length - 9) : name;
            if(cleanName == attributeName && attr.ConstructorArguments.Length > 0)
                return attr.ConstructorArguments[0].Value?.ToString();

        }
        return null;
    }

    private static string? GetHttpVerb (IMethodSymbol methodSymbol)
    {
        string [] verbs = ["HttpGet", "HttpPost", "HttpPut", "HttpDelete", "HttpPatch"];
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


}