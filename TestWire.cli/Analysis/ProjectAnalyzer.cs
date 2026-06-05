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

        // Open the real .csproj using MSBuild — this gives us full type resolution
        // across all referenced projects and NuGet packages
        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(csprojPath);
        var compilation = await project.GetCompilationAsync();

        if (compilation == null)
            throw new Exception("Failed to compile project.");

        foreach (var document in project.Documents)
        {
            // Skip generated files in the obj/ folder — we only want user-written code
            if (document.FilePath?.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") == true)
                continue;

            var syntaxTree = await document.GetSyntaxTreeAsync();
            if (syntaxTree == null) continue;

            var root = await syntaxTree.GetRootAsync();

            // SemanticModel ties this file's syntax to the full compilation
            // This is what lets us resolve types across the entire project
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            // Find all classes ending in "Controller" in this file
            var classDeclarations = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => c.Identifier.Text.EndsWith("Controller"));

            foreach (var classDecl in classDeclarations)
            {
                // Cross from syntax → symbol — now we have full compiler knowledge
                var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                if (classSymbol == null) continue;

                // Fix: prevent duplicate processing when a controller is split
                // across multiple partial class files — only process the primary declaration
                var primaryTree = classSymbol.Locations
                    .FirstOrDefault(l => l.IsInSource)?.SourceTree;
                if (primaryTree != null && classDecl.SyntaxTree != primaryTree)
                    continue;

                var controllerInfo = new ControllerInfo
                {
                    ClassName = classSymbol.Name,
                    Namespace = classSymbol.ContainingNamespace?.ToDisplayString(),
                    BaseRoute = GetAttributeArgument(classSymbol, "Route") ?? string.Empty,
                    Dependencies = GetConstructorDependencies(classSymbol),
                };

                // Iterate methods via symbol — not syntax — for full attribute resolution
                foreach (var member in classSymbol.GetMembers().OfType<IMethodSymbol>())
                {
                    // Only process methods decorated with an HTTP verb attribute
                    var verb = GetHttpVerb(member);
                    if (verb is null) continue;

                    // --- Three-Layer Return Type Resolution ---

                    // Layer 1: read directly from the method signature
                    // e.g. Task<ActionResult<ProductDto>> → "ProductDto"
                    var originalType = member.ReturnType;
                    var unwrapped = UnwrapReturnType(originalType);
                    var returnTypeKind = ReturnTypeKind.Unknown;

                    if (!string.IsNullOrEmpty(unwrapped))
                    {
                        // Only mark as ActionResultOfT if the signature actually
                        // contained ActionResult<T> — not a plain DTO return type
                        var originalName = (originalType as INamedTypeSymbol)?.Name ?? string.Empty;
                        returnTypeKind = originalName.Contains("ActionResult")
                            ? ReturnTypeKind.ActionResultOfT
                            : ReturnTypeKind.IActionResultWithInferredT;
                    }

                    // Layer 2: check [ProducesResponseType(typeof(T), 200)] attribute
                    // catches IActionResult methods that declare their type via attribute
                    if (string.IsNullOrEmpty(unwrapped))
                    {
                        unwrapped = GetProducesResponseDetails(member)
                            .FirstOrDefault(r => r.StatusCode == 200)?.TypeName
                            ?? string.Empty;

                        if (!string.IsNullOrEmpty(unwrapped))
                            returnTypeKind = ReturnTypeKind.IActionResultWithInferredT;
                    }

                    // Layer 3: crawl the method body for return Ok(x), Created(x) etc.
                    // asks the Semantic Model what type x is at each return statement
                    if (string.IsNullOrEmpty(unwrapped))
                    {
                        unwrapped = await TryInferReturnTypeFromBody(member, semanticModel)
                                    ?? string.Empty;

                        if (!string.IsNullOrEmpty(unwrapped))
                            returnTypeKind = ReturnTypeKind.IActionResultWithInferredT;
                    }

                    // --- Build Endpoint ---

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
                        ReturnTypeKind = returnTypeKind,
                        ProducesResponses = GetProducesResponseDetails(member),
                        Parameters = new List<ParameterDetail>()
                    };

                    // --- Build Parameters ---

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

                        // If the parameter is a user-defined class (DTO, Command, etc.)
                        // read its public properties so the generator can build request objects
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
            // Fix: skip indexers and get-only properties — the generator writes object
            // initializers, so any property without a public setter produces uncompilable tests
            if (member.IsIndexer) continue;

            var setMethod = member.SetMethod;
            if (member.DeclaredAccessibility == Accessibility.Public
                && !member.IsStatic
                && setMethod is not null
                && setMethod.DeclaredAccessibility == Accessibility.Public)
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

        // Pick the constructor with the most parameters — that's the DI constructor
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
        // These are the wrapper types we peel off to get to the real return type
        string[] wrappers = ["Task", "ActionResult", "IActionResult"];

        if (typeSymbol is not INamedTypeSymbol namedType)
            return typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        if (wrappers.Contains(namedType.Name))
        {
            // Has a generic argument — recurse to unwrap the next layer
            // e.g. Task<ActionResult<ProductDto>> → ActionResult<ProductDto> → ProductDto
            return namedType.TypeArguments.Length > 0
                ? UnwrapReturnType(namedType.TypeArguments[0])
                : string.Empty; // plain Task / IActionResult — no type info
        }

        return namedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            var name = attr.AttributeClass?.Name;
            if (name == null) continue;

            var cleanName = name.EndsWith("Attribute")
                ? name.Substring(0, name.Length - 9)
                : name;

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

            var cleanName = name.EndsWith("Attribute")
                ? name.Substring(0, name.Length - 9)
                : name;

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
        // Primitives (int, string, bool etc.) — stop
        if (typeSymbol.SpecialType != SpecialType.None)
            return false;

        // System/Microsoft framework types — stop
        var ns = typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        if (ns.StartsWith("System") || ns.StartsWith("Microsoft"))
            return false;

        // Only user-defined classes and structs qualify
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

            var cleanName = name.EndsWith("Attribute")
                ? name.Substring(0, name.Length - 9)
                : name;

            if (cleanName != "ProducesResponseType") continue;
            if (attr.ConstructorArguments.Length == 0) continue;

            var detail = new ProducesResponseDetail();

            // Case 1: [ProducesResponseType(typeof(ProductDto), 200)]
            if (attr.ConstructorArguments[0].Kind == TypedConstantKind.Type)
            {
                var typeSymbol = attr.ConstructorArguments[0].Value as ITypeSymbol;
                detail.TypeName = typeSymbol?
                    .ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                if (attr.ConstructorArguments.Length > 1)
                    detail.StatusCode = (int)(attr.ConstructorArguments[1].Value ?? 200);
            }
            // Case 2: [ProducesResponseType(404)]
            else if (attr.ConstructorArguments[0].Kind == TypedConstantKind.Primitive)
            {
                detail.StatusCode = (int)(attr.ConstructorArguments[0].Value ?? 200);
                detail.TypeName = null;
            }

            result.Add(detail);
        }

        return result;
    }

    private static async Task<string?> TryInferReturnTypeFromBody(
        IMethodSymbol methodSymbol,
        SemanticModel semanticModel)
    {
        var syntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxRef == null) return null;

        var methodSyntax = await syntaxRef.GetSyntaxAsync() as MethodDeclarationSyntax;
        if (methodSyntax == null) return null;

        // Fix: handle both block bodies { return Ok(x); }
        // and expression bodies => Ok(x);
        var searchRoot = (SyntaxNode?)methodSyntax.Body ?? methodSyntax.ExpressionBody;
        if (searchRoot == null) return null;

        var resultMethods = new HashSet<string>
            { "Ok", "Created", "CreatedAtAction", "CreatedAtRoute", "Accepted", "AcceptedAtAction" };

        // Search for ALL invocations — covers both return statements and expression bodies
        var invocations = searchRoot
            .DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            // Extract method name — handles both Ok(x) and this.Ok(x)
            var methodName = invocation.Expression is MemberAccessExpressionSyntax memberAccess
                ? memberAccess.Name.Identifier.Text
                : (invocation.Expression as IdentifierNameSyntax)?.Identifier.Text;

            if (methodName is null || !resultMethods.Contains(methodName)) continue;

            var arg = invocation.ArgumentList.Arguments.FirstOrDefault();
            if (arg == null) continue;

            // Ask the compiler what type this argument expression resolves to
            var typeInfo = semanticModel.GetTypeInfo(arg.Expression);
            if (typeInfo.Type is null) continue;

            return typeInfo.Type
                .ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }

        return null;
    }
}