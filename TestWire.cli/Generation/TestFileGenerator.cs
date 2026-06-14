<<<<<<< Updated upstream
=======
using System.Net.Http.Headers;
>>>>>>> Stashed changes
using System.Text;
using TestWire.cli.Analysis;

namespace TestWire.cli.Generation;

public class TestFileGenerator
{
<<<<<<< Updated upstream
    public static string Generate(ControllerInfo controller, string framework)
    {
        var sb = new StringBuilder();

        // usings
        sb.AppendLine("using System;");
        sb.AppendLine("using Moq;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
        sb.AppendLine("using Microsoft.AspNetCore.Http;");
        var projectNamespace = controller.Namespace.Replace(".Controllers", "");
        sb.AppendLine($"using {projectNamespace}.DTOs;");
        sb.AppendLine(framework == "nunit" ? "using NUnit.Framework;" : "using Xunit;");
        sb.AppendLine();

        // namespace
        sb.AppendLine($"namespace {controller.Namespace}.Tests;");
=======
    public static string Generate(ControllerInfo controller, GenerationContext context)
    {
        var sb = new StringBuilder();

        // Usings for the generated test file
        if (context.Framework == TestFramework.NUnit)
            sb.AppendLine("using NUnit.Framework;");
        else
            sb.AppendLine("using Xunit;");

        sb.AppendLine("using System.Net;");
        sb.AppendLine("using System.Net.Http.Json;");
        sb.AppendLine("using System.Net.Http.Headers;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc.Testing;");
        sb.AppendLine($"using {context.ProjectNamespace};");
        sb.AppendLine();

        // Namespace of the generated test file
        sb.AppendLine($"namespace {context.ProjectNamespace}.Tests;");
>>>>>>> Stashed changes
        sb.AppendLine();

        // class
        sb.AppendLine($"public class {controller.ClassName}Tests");
        sb.AppendLine("{");

        // one test per endpoint
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

    private static bool IsLoggerDependency(string typeName)
    {
        // Strip generic arguments first so dots inside ILogger<TCategoryName> do not affect
        // extraction of the outer type name from fully-qualified names.
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

        // Match ILogger and ILogger<T> but NOT ILoggerFactory or ILoggerProvider
        return lastSegment == "ILogger";
    }

    private static void WriteControllerSetup(StringBuilder sb, ControllerInfo controller)
    {
        if (controller.Dependencies.Count == 0)
        {
            sb.AppendLine($"        var controller = new {controller.ClassName}();");
            return;
        }

        // Generate mock variables — skip ILogger (infrastructure noise, not worth asserting)
        foreach (var dep in controller.Dependencies)
        {
            if (IsLoggerDependency(dep.Type))
            {
                sb.AppendLine($"        // ILogger suppressed by TestWire");
                continue;
            }

            var mockName = $"mock{char.ToUpper(dep.Name[0])}{dep.Name.Substring(1)}";
            sb.AppendLine($"        var {mockName} = new Mock<{dep.Type}>();");
        }

        sb.AppendLine();

        // Build constructor args — ILogger gets Mock.Of<T>() inline instead of a named variable
        var args = string.Join(", ", controller.Dependencies.Select(dep =>
            IsLoggerDependency(dep.Type)
                ? $"Mock.Of<{dep.Type}>()"
                : $"mock{char.ToUpper(dep.Name[0])}{dep.Name.Substring(1)}.Object"));

        sb.AppendLine($"        var controller = new {controller.ClassName}({args});");
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
            p.DtoProperties.Count > 0
                ? BuildObjectInitializer(p.FullyQualifiedType, p.DtoProperties)
                : GetDefaultValue(p.Type)));

        sb.AppendLine($"    {testAttr}");
        sb.AppendLine($"    public {asyncKeyword} {testName}()");
        sb.AppendLine("    {");
        sb.AppendLine($"        // Arrange");
        WriteControllerSetup(sb, controller);
        sb.AppendLine();
        sb.AppendLine($"        // Act");
        sb.AppendLine($"        var result = {awaitKeyword}controller.{endpoint.MethodName}({paramValues});");
        sb.AppendLine();
        sb.AppendLine($"        // Assert");
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
            p.DtoProperties.Count > 0
                ? BuildObjectInitializer(p.FullyQualifiedType, p.DtoProperties)
                : GetInvalidValue(p.Type)));

        sb.AppendLine($"    {testAttr}");
        sb.AppendLine($"    public {asyncKeyword} {testName}()");
        sb.AppendLine("    {");
        sb.AppendLine($"        // Arrange");
        WriteControllerSetup(sb, controller);
        sb.AppendLine();
        sb.AppendLine($"        // Act");
        sb.AppendLine($"        var result = {awaitKeyword}controller.{endpoint.MethodName}({paramValues});");
        sb.AppendLine();
        sb.AppendLine($"        // Assert");
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
            p.DtoProperties.Count > 0
                ? BuildObjectInitializer(p.FullyQualifiedType, p.DtoProperties)
                : GetDefaultValue(p.Type)));

        sb.AppendLine($"    {testAttr}");
        sb.AppendLine($"    public {asyncKeyword} {testName}()");
        sb.AppendLine("    {");
        sb.AppendLine($"        // Arrange");
        WriteControllerSetup(sb, controller);
        sb.AppendLine($"        controller.ControllerContext = new ControllerContext();");
        sb.AppendLine($"        controller.ControllerContext.HttpContext = new DefaultHttpContext();");
        sb.AppendLine();
        sb.AppendLine($"        // Act");
        sb.AppendLine($"        var result = {awaitKeyword}controller.{endpoint.MethodName}({paramValues});");
        sb.AppendLine();
        sb.AppendLine($"        // Assert");
        sb.AppendLine($"        Assert.IsType<UnauthorizedResult>(result);");
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
        "decimal" => "1.0M",
        "double" or "float" => "1.0",
        _ => "null"
    };

    private static string GetInvalidValue(string type) => type.ToLower() switch
    {
        "int" or "int32" or "int64" or "long" => "-1",
        "string" => "null",
        "bool" or "boolean" => "false",
        "guid" => "Guid.Empty",
        "datetime" => "DateTime.MinValue",
        "decimal" => "-1.0M",
        "double" or "float" => "-1.0",
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