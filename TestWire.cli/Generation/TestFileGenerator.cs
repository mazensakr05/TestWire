using System.Text;
using TestWire.cli.Analysis;


namespace TestWire.cli.Generation;

public class TestFileGenerator
{
    public static string Generate(ControllerInfo controller, string framework)
    {
        var sb = new StringBuilder();
        
        // using 
        sb.AppendLine("using System;");
        sb.AppendLine("using Moq;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
        sb.AppendLine(framework == "nunit" ? "using NUnit.Framework;" : "using Xunit;");        sb.AppendLine();
        
        // NameSpace 
        sb.AppendLine($"namespace {controller.Namespace}.Tests;");
        sb.AppendLine();
        
        // Class 
        sb.AppendLine($"public class {controller.ClassName}Tests");
        sb.AppendLine("{");
        
        // one Test Per EndPoint 
        foreach (var endpoint in controller.Endpoints)
        {
            var testAttr = framework == "nunit" ? "[Test]" : "[Fact]";

            WriteHappyPath(sb, endpoint, controller.ClassName, testAttr);
            WriteBadPath(sb, endpoint, controller.ClassName, testAttr);

            if (endpoint.HasAuthorize)
                WriteSecurityTest(sb, endpoint, controller.ClassName, testAttr);
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void WriteHappyPath(StringBuilder sb, EndpointInfo endpoint, string className, string testAttr)
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
                ? BuildObjectInitializer(p.Type, p.DtoProperties)
                : GetDefaultValue(p.Type)));        sb.AppendLine($"    {testAttr}");
        sb.AppendLine($"    public {asyncKeyword} {testName}()");
        sb.AppendLine("    {");
        sb.AppendLine($"        // Arrange");
        sb.AppendLine($"        var controller = new {className}();");
        sb.AppendLine();
        sb.AppendLine($"        // Act");
        sb.AppendLine($"        var result = {awaitKeyword}controller.{endpoint.MethodName}({paramValues});");
        sb.AppendLine();
        sb.AppendLine($"        // Assert");
        sb.AppendLine($"        Assert.IsType<{expectedResult}>(result);");
        sb.AppendLine("    }");
        sb.AppendLine();

    }
    
    private static void WriteBadPath(StringBuilder sb, EndpointInfo endpoint, string className, string testAttr)
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
                ? BuildObjectInitializer(p.Type, p.DtoProperties)
                : GetDefaultValue(p.Type)));
        sb.AppendLine($"    {testAttr}");
        sb.AppendLine($"    public {asyncKeyword} {testName}()");
        sb.AppendLine("    {");
        sb.AppendLine($"        // Arrange");
        sb.AppendLine($"        var controller = new {className}();");
        sb.AppendLine();
        sb.AppendLine($"        // Act");
        sb.AppendLine($"        var result = {awaitKeyword}controller.{endpoint.MethodName}({paramValues});");
        sb.AppendLine();
        sb.AppendLine($"        // Assert");
        sb.AppendLine($"        Assert.IsType<{expectedResult}>(result);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }
    private static void WriteSecurityTest(StringBuilder sb, EndpointInfo endpoint, string className, string testAttr)
    {
        var testName = $"{endpoint.MethodName}_Returns401_WhenUnauthorized";
        var asyncKeyword = endpoint.IsAsync ? "async Task" : "void";
        var awaitKeyword = endpoint.IsAsync ? "await " : "";
        var paramValues = string.Join(", ", endpoint.Parameters.Select(p => GetDefaultValue(p.Type)));

        sb.AppendLine($"    {testAttr}");
        sb.AppendLine($"    public {asyncKeyword} {testName}()");
        sb.AppendLine("    {");
        sb.AppendLine($"        // Arrange");
        sb.AppendLine($"        var controller = new {className}();");
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
        "int" or "int32" or "int64" or "long"   => "1",
        "string"                                 => "\"test\"",
        "bool" or "boolean"                      => "true",
        "guid"                                   => "Guid.NewGuid()",
        "datetime"                               => "DateTime.UtcNow",
        "double" or "float" or "decimal"         => "1.0",
        _                                        => "null" // no longer Called for DTOs - Handled Below 
    };

    private static string GetInvalidValue(string type) => type.ToLower() switch
    {
        "int" or "int32" or "int64" or "long"   => "-1",
        "string"                                 => "null",
        "bool" or "boolean"                      => "false",
        "guid"                                   => "Guid.Empty",
        "datetime"                               => "DateTime.MinValue",
        "double" or "float" or "decimal"         => "-1.0",
        _                                        => "null"
    };

    private static string BuildObjectInitializer(string typeName, List<PropertyDetail> properties)
    {
        if (properties.Count == 0)
        {
            return $"new {typeName}()";
            
        }

        var props = string.Join(", ", properties.Select(p => $"{p.Name} = {GetDefaultValue(p.Type)}"));
        return $"new {typeName}({props})";
    }
}