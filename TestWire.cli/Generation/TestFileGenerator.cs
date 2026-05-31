using System.Text;
using TestWire.cli.Analysis;

namespace TestWire.cli.Generation;

public class TestFileGenerator
{
    public static string Generate(ControllerInfo controller, string framework)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Security.Claims;");
        sb.AppendLine("using Microsoft.AspNetCore.Authorization;");
        sb.AppendLine("using Microsoft.AspNetCore.Http;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc.Abstractions;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc.Controllers;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc.Filters;");
        if (controller.Endpoints.Any(e => e.Parameters.Any(p => p.IsCancellationToken)))
            sb.AppendLine("using System.Threading;");
        var hasNonLoggerDeps = controller.Dependencies.Any(d => !IsLoggerDependency(d.Type));

        if (hasNonLoggerDeps)
            sb.AppendLine("using Moq;");

        sb.AppendLine(framework == "nunit" ? "using NUnit.Framework;" : "using Xunit;");
        // Derive DTO namespace from controller namespace convention
        // e.g., SampleApi.Controllers → SampleApi.DTOs
        var controllerNs = controller.Namespace;
        var dtoNs = controllerNs;
        var controllersIdx = controllerNs.LastIndexOf(".Controllers");
        if (controllersIdx >= 0)
        {
            dtoNs = controllerNs.Substring(0, controllersIdx) + ".DTOs";
        }

        // Check if any endpoint uses DTO types
        var hasDtoParams = controller.Endpoints
            .SelectMany(e => e.Parameters)
            .Any(p => p.DtoProperties.Count > 0);

        if (hasDtoParams && dtoNs != controllerNs)
        {
            sb.AppendLine($"using {dtoNs};");
        }

        sb.AppendLine();
        sb.AppendLine($"namespace {controller.Namespace}.Tests;");
        sb.AppendLine();
        sb.AppendLine($"public class {controller.ClassName}Tests");
        sb.AppendLine("{");

        foreach (var endpoint in controller.Endpoints)
        {
            var testAttr = framework == "nunit" ? "[Test]" : "[Fact]";

            WriteHappyPath(sb, endpoint, controller, testAttr);
            WriteBadPath(sb, endpoint, controller, testAttr);

            if (endpoint.HasAuthorize)
                WriteSecurityTest(sb, endpoint, controller, testAttr);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string MockVarName(string depName)
    {
        return $"mock{char.ToUpper(depName[0])}{depName.Substring(1)}";
    }

    private static bool IsLoggerDependency(string typeName)
    {
        var outerType = typeName;
        var genericStart = outerType.IndexOf('<');
        if (genericStart >= 0)
            outerType = outerType.Substring(0, genericStart);

        var lastDot = outerType.LastIndexOf('.');
        var lastAliasSeparator = outerType.LastIndexOf("::");
        var separatorIndex = Math.Max(lastDot, lastAliasSeparator);
        var lastSegment = separatorIndex >= 0
            ? outerType.Substring(separatorIndex + (separatorIndex == lastAliasSeparator ? 2 : 1))
            : outerType;

        return lastSegment == "ILogger";
    }

    private static void WriteControllerSetup(StringBuilder sb, ControllerInfo controller)
    {
        if (controller.Dependencies.Count == 0)
        {
            sb.AppendLine($"        var controller = new {controller.ClassName}();");
            return;
        }

        foreach (var dep in controller.Dependencies)
        {
            if (IsLoggerDependency(dep.Type))
            {
                sb.AppendLine($"        // ILogger suppressed by TestWire");
                continue;
            }

            var mockName = MockVarName(dep.Name);
            sb.AppendLine($"        var {mockName} = new Mock<{dep.Type}>();");
        }

        sb.AppendLine();

        var args = string.Join(", ", controller.Dependencies.Select(dep =>
            IsLoggerDependency(dep.Type)
                ? $"Mock.Of<{dep.Type}>()"
                : $"{MockVarName(dep.Name)}.Object"));

        sb.AppendLine($"        var controller = new {controller.ClassName}({args});");
    }

    private static string GetHappyPathReturnValue(string returnType)
    {
        var clean = returnType.Replace("?", "").Trim();
        var lower = clean.ToLower();
        return lower switch
        {
            "int" or "int32" or "int64" or "long" => "1",
            "string" => "\"test\"",
            "bool" or "boolean" => "true",
            "guid" => "Guid.NewGuid()",
            "datetime" => "DateTime.UtcNow",
            "double" => "1.0",
            "float" => "1.0F",
            "decimal" => "1.0m",
            "void" or "" => "new object()",
            _ when lower.Contains("list<") => $"new {ExtractCollectionType(clean, "System.Collections.Generic.List")}()",
            _ when lower.Contains("ilist<") => $"new {ExtractCollectionType(clean, "System.Collections.Generic.List")}()",
            _ when lower.Contains("icollection<") => $"new {ExtractCollectionType(clean, "System.Collections.Generic.List")}()",
            _ when lower.Contains("ienumerable<") => $"new {ExtractCollectionType(clean, "System.Collections.Generic.List")}()",
            _ => $"new {CleanTypeName(clean)}()"
        };
    }

    private static string ExtractCollectionType(string returnType, string collectionType)
    {
        var open = returnType.IndexOf('<');
        var close = returnType.LastIndexOf('>');
        if (open >= 0 && close > open)
        {
            var inner = returnType.Substring(open + 1, close - open - 1);
            return $"{collectionType}<{inner}>";
        }
        return returnType;
    }

    private static string CleanTypeName(string typeName)
    {
        var idx = typeName.IndexOf('<');
        if (idx >= 0)
            return typeName.Substring(0, idx);
        return typeName;
    }

    private static string GetFailureValue(string returnType)
    {
        return returnType.ToLower() switch
        {
            "int" or "int32" or "int64" or "long" => "0",
            "string" => "null",
            "bool" or "boolean" => "false",
            "guid" => "Guid.Empty",
            "datetime" => "DateTime.MinValue",
            "double" => "0.0",
            "float" => "0.0F",
            "decimal" => "0.0m",
            "void" or "" => "new object()",
            _ => "null"
        };
    }

    private static string GetHappyPathMockReturn(string returnType, bool isAsync)
    {
        if (!isAsync)
            return $"Returns({GetHappyPathReturnValue(returnType)})";

        if (returnType == "void" || returnType == "")
            return "Returns(System.Threading.Tasks.Task.CompletedTask)";

        var value = GetHappyPathReturnValue(returnType);
        return $"ReturnsAsync(() => {value})";
    }

    private static string GetBadPathMockReturn(string returnType, bool isAsync)
    {
        if (!isAsync)
            return $"Returns({GetFailureValue(returnType)})";

        if (returnType == "void" || returnType == "")
            return "Returns(System.Threading.Tasks.Task.CompletedTask)";

        var value = GetFailureValue(returnType);
        var castType = returnType.Replace("?", "");
        if (value == "null")
            return $"ReturnsAsync(({castType})null)";
        return $"ReturnsAsync({value})";
    }

    private static string MockSetupLine(string mockName, DependencyCallInfo call, string returnsExpr)
    {
        var paramSetup = string.Join(", ", call.ArgumentTypes.Select(t => $"It.IsAny<{t}>()"));
        return $"        {mockName}.Setup(x => x.{call.MethodName}({paramSetup})).{returnsExpr};";
    }

    private static void WriteHappyPathMockSetups(StringBuilder sb, EndpointInfo endpoint, ControllerInfo controller)
    {
        foreach (var call in endpoint.DependencyCalls)
        {
            if (IsLoggerDependency(call.DependencyType)) continue;

            var mockName = MockVarName(call.DependencyName);
            var returnsExpr = GetHappyPathMockReturn(call.ReturnType, call.IsAsync);
            sb.AppendLine(MockSetupLine(mockName, call, returnsExpr));
        }
    }

    private static void WriteBadPathMockSetups(StringBuilder sb, EndpointInfo endpoint, ControllerInfo controller)
    {
        foreach (var call in endpoint.DependencyCalls)
        {
            if (IsLoggerDependency(call.DependencyType)) continue;

            var mockName = MockVarName(call.DependencyName);
            var returnsExpr = GetBadPathMockReturn(call.ReturnType, call.IsAsync);
            sb.AppendLine(MockSetupLine(mockName, call, returnsExpr));
        }
    }

    private static void WriteHappyPath(StringBuilder sb, EndpointInfo endpoint, ControllerInfo controller, string testAttr)
    {
        var expectedResult = endpoint.HttpVerb switch
        {
            "HttpGet" => "OkObjectResult",
            "HttpPost" => "CreatedAtActionResult",
            "HttpPut" => "OkObjectResult",
            "HttpDelete" => "NoContentResult",
            _ => "OkObjectResult"
        };

        var testName = $"{endpoint.MethodName}_Returns{expectedResult.Replace("Result", "")}_WhenSuccessful";
        var asyncKeyword = endpoint.IsAsync ? "async Task" : "void";
        var awaitKeyword = endpoint.IsAsync ? "await " : "";
        var paramValues = string.Join(", ", endpoint.Parameters.Select(p =>
            p.IsCancellationToken ? "CancellationToken.None":
            p.DtoProperties.Count > 0
                ? BuildObjectInitializer(p.Type, p.DtoProperties)
                : GetDefaultValue(p.Type)));

        sb.AppendLine($"    {testAttr}");
        sb.AppendLine($"    public {asyncKeyword} {testName}()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Arrange");
        WriteControllerSetup(sb, controller);
        WriteHappyPathMockSetups(sb, endpoint, controller);
        sb.AppendLine();
        sb.AppendLine("        // Act");
        sb.AppendLine($"        var result = {awaitKeyword}controller.{endpoint.MethodName}({paramValues});");
        sb.AppendLine();
        sb.AppendLine("        // Assert");
        sb.AppendLine($"        Assert.IsType<{expectedResult}>(result);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void WriteBadPath(StringBuilder sb, EndpointInfo endpoint, ControllerInfo controller, string testAttr)
    {
        var expectedResult = endpoint.HttpVerb switch
        {
            "HttpGet" => "NotFoundResult",
            "HttpPost" => "BadRequestObjectResult",
            "HttpPut" => "BadRequestObjectResult",
            "HttpDelete" => "NotFoundResult",
            _ => "BadRequestObjectResult"
        };

        var testName = $"{endpoint.MethodName}_Returns{expectedResult.Replace("Result", "")}_WhenFailed";
        var asyncKeyword = endpoint.IsAsync ? "async Task" : "void";
        var awaitKeyword = endpoint.IsAsync ? "await " : "";
        var paramValues = string.Join(", ", endpoint.Parameters.Select(p =>
            p.IsCancellationToken ? "CancellationToken.None":
            p.DtoProperties.Count > 0
                ? BuildObjectInitializer(p.Type, p.DtoProperties)
                : GetInvalidValue(p.Type)));

        sb.AppendLine($"    {testAttr}");
        sb.AppendLine($"    public {asyncKeyword} {testName}()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Arrange");
        WriteControllerSetup(sb, controller);
        WriteBadPathMockSetups(sb, endpoint, controller);
        sb.AppendLine();
        sb.AppendLine("        // Act");
        sb.AppendLine($"        var result = {awaitKeyword}controller.{endpoint.MethodName}({paramValues});");
        sb.AppendLine();
        sb.AppendLine("        // Assert");
        sb.AppendLine($"        Assert.IsType<{expectedResult}>(result);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void WriteSecurityTest(StringBuilder sb, EndpointInfo endpoint, ControllerInfo controller, string testAttr)
    {
        var testName = $"{endpoint.MethodName}_Returns401_WhenUnauthorized";
        var asyncKeyword = endpoint.IsAsync ? "async Task" : "void";
        var awaitKeyword = endpoint.IsAsync ? "await " : "";
        var paramValues = string.Join(", ", endpoint.Parameters.Select(p =>
            p.IsCancellationToken ? "CancellationToken.None":
            p.DtoProperties.Count > 0
                ? BuildObjectInitializer(p.Type, p.DtoProperties)
                : GetDefaultValue(p.Type)));

        sb.AppendLine($"    {testAttr}");
        sb.AppendLine($"    public {asyncKeyword} {testName}()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Arrange");
        WriteControllerSetup(sb, controller);
        sb.AppendLine();
        sb.AppendLine("        var authorizationFilter = new Mock<IAuthorizationFilter>();");
        sb.AppendLine("        authorizationFilter");
        sb.AppendLine("            .Setup(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()))");
        sb.AppendLine("            .Callback((AuthorizationFilterContext ctx) =>");
        sb.AppendLine("            {");
        sb.AppendLine("                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());");
        sb.AppendLine("                ctx.HttpContext.User = anonymous;");
        sb.AppendLine("                ctx.Result = new UnauthorizedResult();");
        sb.AppendLine("            });");
        sb.AppendLine();
        sb.AppendLine("        var controllerContext = new ControllerContext");
        sb.AppendLine("        {");
        sb.AppendLine("            HttpContext = new DefaultHttpContext(),");
        sb.AppendLine("            RouteData = new Microsoft.AspNetCore.Routing.RouteData(),");
        sb.AppendLine("            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor");
        sb.AppendLine("            {");
        sb.AppendLine("                EndpointMetadata = new List<object> { new AuthorizeAttribute() }");
        sb.AppendLine("            }");
        sb.AppendLine("        };");
        sb.AppendLine("        controller.ControllerContext = controllerContext;");
        sb.AppendLine();
        sb.AppendLine("        var authFilterContext = new AuthorizationFilterContext(");
        sb.AppendLine("            controllerContext, new IFilterMetadata[] { });");
        sb.AppendLine("        authorizationFilter.Object.OnAuthorization(authFilterContext);");
        sb.AppendLine();
        sb.AppendLine("        if (authFilterContext.Result != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            Assert.IsType<UnauthorizedResult>(authFilterContext.Result);");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // Act");
        sb.AppendLine($"        var result = {awaitKeyword}controller.{endpoint.MethodName}({paramValues});");
        sb.AppendLine();
        sb.AppendLine("        // Assert");
        sb.AppendLine("        Assert.IsType<UnauthorizedResult>(result);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static string GetDefaultValue(string type) => type.ToLower() switch
    {
        "int" or "int32" or "int64" or "long" => "1",
        "string" => "\"test\"",
        "bool" or "boolean" => "true",
        "guid" => "Guid.NewGuid()",
        "datetime" => "DateTime.UtcNow",
        "double" => "1.0",
        "float" => "1.0F",
        "decimal" => "1.0m",
        _ => "null"
    };

    private static string GetInvalidValue(string type) => type.ToLower() switch
    {
        "int" or "int32" or "int64" or "long" => "-1",
        "string" => "null",
        "bool" or "boolean" => "false",
        "guid" => "Guid.Empty",
        "datetime" => "DateTime.MinValue",
        "double" => "-1.0",
        "float" => "-1.0F",
        "decimal" => "-1.0m",
        _ => "null"
    };

    private static string BuildObjectInitializer(string typeName, List<PropertyDetail> properties)
    {
        if (properties.Count == 0)
            return $"new {typeName}()";

        var props = string.Join(", ", properties.Select(p =>
            $"{p.Name} = {GetDefaultValue(p.Type)}"));

        return $"new {typeName} {{ {props} }}";
    }
}
