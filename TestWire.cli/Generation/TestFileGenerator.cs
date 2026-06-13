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
        sb.AppendLine($"using {projectNamespace}.Models;");
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
            // Step 1: Build the happy path URL
            var url = RouteBuilder.Build(
                controller.BaseRoute,
                controller.ClassName,
                endpoint.Route,
                endpoint.Parameters);

            // Step 2: Happy path test — pass isNUnit directly (Bug #1 fix)
            sb.Append(MethodBodyBuilder.Build(endpoint, url, isNUnit));

            // Step 3: 401 test — only if endpoint requires auth
            if (endpoint.HasAuthorize)
                sb.Append(BuildUnauthorizedTest(endpoint, url, isNUnit));

            // Step 4: 404 test — only if endpoint is the right verb AND has route params
            if (ShouldGenerate404Test(endpoint))
                sb.Append(MethodBodyBuilder.BuildNotFoundTest(
                    endpoint,
                    controller.BaseRoute,
                    controller.ClassName,
                    isNUnit));
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

    private static bool ShouldGenerate404Test(EndpointInfo endpoint)
    {
        // Only these verbs make sense for a 404 test
        // POST is excluded — POST creates resources, doesn't look them up by ID
        var supportedVerbs = new[] { "HttpGet", "HttpPut", "HttpDelete", "HttpPatch" };

        // Must be a supported verb AND must have at least one route parameter
        // No route param = no ID = no way to get a 404 by ID
        return supportedVerbs.Contains(endpoint.HttpVerb)
            && endpoint.Parameters.Any(p => p.IsFromRoute);
    }

}