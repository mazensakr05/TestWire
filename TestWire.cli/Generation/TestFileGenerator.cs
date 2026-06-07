using System.Text;
using TestWire.cli.Analysis;

namespace TestWire.cli.Generation;

public static class TestFileGenerator
{
    public static string Generate(ControllerInfo controller, string framework = "xunit")
    {
        var isNUnit = framework.Equals("nunit", StringComparison.OrdinalIgnoreCase);
        var sb = new StringBuilder();

        // Usings
        sb.AppendLine(isNUnit ? "using NUnit.Framework;" : "using Xunit;");
        sb.AppendLine("using System.Net;");
        sb.AppendLine("using System.Net.Http.Json;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc.Testing;");
        var projectNamespace = controller.Namespace.Replace(".Controllers", "");
        sb.AppendLine($"using {projectNamespace};");
        sb.AppendLine($"using {projectNamespace}.DTOs;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine("namespace TestWire.Generated.Tests;");
        sb.AppendLine();

        // Class declaration
        if (isNUnit)
        {
            sb.AppendLine("[TestFixture]");
            sb.AppendLine($"public class {controller.ClassName}Tests");
        }
        else
        {
            sb.AppendLine($"public class {controller.ClassName}Tests : IClassFixture<WebApplicationFactory<Program>>");
        }
        
        sb.AppendLine("{");

        // Fields and constructor
        sb.AppendLine("    private readonly HttpClient _client;");
        sb.AppendLine();

        if (isNUnit)
        {
            sb.AppendLine("    private WebApplicationFactory<Program> _factory;");
            sb.AppendLine();
            sb.AppendLine("    [SetUp]");
            sb.AppendLine("    public void SetUp()");
            sb.AppendLine("    {");
            sb.AppendLine("        _factory = new WebApplicationFactory<Program>();");
            sb.AppendLine("        _client = _factory.CreateClient();");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [TearDown]");
            sb.AppendLine("    public void TearDown()");
            sb.AppendLine("    {");
            sb.AppendLine("        _client.Dispose();");
            sb.AppendLine("        _factory.Dispose();");
            sb.AppendLine("    }");
        }
        else
        {
            sb.AppendLine($"    public {controller.ClassName}Tests(WebApplicationFactory<Program> factory)");
            sb.AppendLine("    {");
            sb.AppendLine("        _client = factory.CreateClient();");
            sb.AppendLine("    }");
        }
        sb.AppendLine();

        // Generate one test method per endpoint
        foreach (var endpoint in controller.Endpoints)
        {
            // Step 1: build the real URL for this endpoint
            var url = RouteBuilder.Build(
                controller.BaseRoute,
                controller.ClassName,
                endpoint.Route,
                endpoint.Parameters);

            // Step 2: build the happy path test method
            var testBody = MethodBodyBuilder.Build(endpoint, url);
            if (isNUnit)
            {
                testBody = testBody.Replace("[Fact]", "[Test]")
                                 .Replace("Assert.Equal(", "Assert.That(")
                                 .Replace("Assert.NotNull(", "Assert.That(")
                                 .Replace("Assert.Null(", "Assert.That(");
                                 // Simple replacement for NUnit Assert.That syntax might be complex, 
                                 // let's stick to simple ones or use NUnit Classic Assert if available
            }
            sb.Append(testBody);

            // Step 3: if endpoint requires auth, generate a 401 test too
            if (endpoint.HasAuthorize)
                sb.Append(BuildUnauthorizedTest(endpoint, url, isNUnit));

        }
        // Close class
        sb.AppendLine("}");

        return sb.ToString();
    }
    
    private static string BuildUnauthorizedTest(EndpointInfo endpoint, string url, bool isNUnit)
    {
        var sb = new StringBuilder();

        var verb = endpoint.HttpVerb switch
        {
            "HttpGet" => $"await _client.GetAsync(\"{url}\");",
            "HttpDelete" => $"await _client.DeleteAsync(\"{url}\");",
            "HttpPost" => $"await _client.PostAsJsonAsync(\"{url}\", new {{ }});",
            "HttpPut" => $"await _client.PutAsJsonAsync(\"{url}\", new {{ }});",
            "HttpPatch" => $"await _client.PatchAsJsonAsync(\"{url}\", new {{ }});",
            _ => $"await _client.GetAsync(\"{url}\");"
        };

        sb.AppendLine(isNUnit ? "    [Test]" : "    [Fact]");
        sb.AppendLine($"    public async Task {endpoint.MethodName}_Returns401_WhenUnauthenticated()");
        sb.AppendLine("    {");
        sb.AppendLine($"        var response = {verb}");
        if (isNUnit)
        {
            sb.AppendLine($"        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));");
        }
        else
        {
            sb.AppendLine($"        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);");
        }
        sb.AppendLine("    }");
        sb.AppendLine();

        return sb.ToString();
    }
}