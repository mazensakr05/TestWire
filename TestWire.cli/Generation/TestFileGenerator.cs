using System.Text;
using TestWire.cli.Analysis;

namespace TestWire.cli.Generation;

public static class TestFileGenerator
{
    public static string Generate(ControllerInfo controller)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Xunit;");
        sb.AppendLine("using System.Net;");
        sb.AppendLine("using System.Net.Http.Json;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc.Testing;");
        var projectNamespace = controller.Namespace.Replace(".Controllers", "");
        sb.AppendLine($"using {projectNamespace};");
        sb.AppendLine($"using {projectNamespace}.DTOs;");
        sb.AppendLine($"using {projectNamespace}.Models;");
        sb.AppendLine();

        sb.AppendLine("namespace TestWire.Generated.Tests;");
        sb.AppendLine();

        sb.AppendLine($"public class {controller.ClassName}Tests : IClassFixture<WebApplicationFactory<Program>>");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly HttpClient _client;");
        sb.AppendLine();

        sb.AppendLine($"    public {controller.ClassName}Tests(WebApplicationFactory<Program> factory)");
        sb.AppendLine("    {");
        sb.AppendLine("        _client = factory.CreateClient();");
        sb.AppendLine("    }");
        sb.AppendLine();

        foreach (var endpoint in controller.Endpoints)
        {
            var url = RouteBuilder.Build(
                controller.BaseRoute,
                controller.ClassName,
                endpoint.Route,
                endpoint.Parameters);

            sb.Append(MethodBodyBuilder.Build(endpoint, url));

            if (endpoint.HasAuthorize)
                sb.Append(BuildUnauthorizedTest(endpoint, url));

            if (ShouldGenerate404Test(endpoint))
                sb.Append(MethodBodyBuilder.BuildNotFoundTest(
                    endpoint,
                    controller.BaseRoute,
                    controller.ClassName));
        }
        sb.AppendLine("}");

        return sb.ToString();
    }
    
    private static string BuildUnauthorizedTest(EndpointInfo endpoint, string url)
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

        sb.AppendLine("    [Fact]");
        sb.AppendLine($"    public async Task {endpoint.MethodName}_Returns401_WhenUnauthenticated()");
        sb.AppendLine("    {");
        sb.AppendLine($"        var response = {verb}");
        sb.AppendLine("        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);");
        sb.AppendLine("    }");
        sb.AppendLine();

        return sb.ToString();
    }

    private static bool ShouldGenerate404Test(EndpointInfo endpoint)
    {
        var supportedVerbs = new[] { "HttpGet", "HttpPut", "HttpDelete", "HttpPatch" };

        return supportedVerbs.Contains(endpoint.HttpVerb)
            && endpoint.Parameters.Any(p => p.IsFromRoute);
    }
}