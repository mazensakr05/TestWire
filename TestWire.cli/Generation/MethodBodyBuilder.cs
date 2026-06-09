using System.Text;
using System.Text.RegularExpressions;
using TestWire.cli.Analysis;

namespace TestWire.cli.Generation;

public static class MethodBodyBuilder
{


    public static string Build(EndpointInfo endpoint, string url)
    {
        var sb = new StringBuilder();
        var testName = BuildTestName(endpoint);
        var bodyParam = endpoint.Parameters.FirstOrDefault(p => p.IsFromBody);

        sb.AppendLine("    [Fact]");
        sb.AppendLine($"    public async Task {testName}()");
        sb.AppendLine("    {");

        // ONLY the request object lives inside this if
        if (bodyParam is not null)
        {
            sb.AppendLine($"        var request = new {bodyParam.FullyQualifiedType}");
            sb.AppendLine("        {");
            foreach (var prop in bodyParam.DtoProperties)
            {
                sb.AppendLine($"            {prop.Name} = {GetTestValueForType(prop.Type)},");
            }
            sb.AppendLine("        };");
            sb.AppendLine();
        }

        // HTTP call — always runs
        var httpCall = endpoint.HttpVerb switch
        {
            "HttpGet" => $"await _client.GetAsync(\"{url}\");",
            "HttpDelete" => $"await _client.DeleteAsync(\"{url}\");",
            "HttpPost" => bodyParam is not null
                                ? $"await _client.PostAsJsonAsync(\"{url}\", request);"
                                : $"await _client.PostAsJsonAsync(\"{url}\", new {{ }});",
            "HttpPut" => bodyParam is not null
                                ? $"await _client.PutAsJsonAsync(\"{url}\", request);"
                                : $"await _client.PutAsJsonAsync(\"{url}\", new {{ }});",
            "HttpPatch" => bodyParam is not null
                                ? $"await _client.PatchAsJsonAsync(\"{url}\", request);"
                                : $"await _client.PatchAsJsonAsync(\"{url}\", new {{ }});",
            _ => $"await _client.GetAsync(\"{url}\");"
        };

        sb.AppendLine($"        var response = {httpCall}");

        // Assertions — always runs
        var expectedStatusCode = StatusCodeToHttpStatusCode(endpoint.ExpectedStatusCode);
        sb.AppendLine($"        Assert.Equal({expectedStatusCode}, response.StatusCode);");
        if (!string.IsNullOrEmpty(endpoint.ReturnType))
        {
            sb.AppendLine($"        var result = await response.Content.ReadFromJsonAsync<{endpoint.ReturnType}>();");
            sb.AppendLine("        Assert.NotNull(result);");
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string BuildTestName(EndpointInfo endpoint)
    {
        if (string.IsNullOrEmpty(endpoint.ReturnType))
        {
            return $"{endpoint.MethodName}_Returns{endpoint.ExpectedStatusCode}";
        }

        var sanitizedType = Regex.Replace(endpoint.ReturnType, @"[^a-zA-Z0-9]", "_");
        sanitizedType = Regex.Replace(sanitizedType, @"_+", "_").Trim('_');

        return $"{endpoint.MethodName}_Returns{endpoint.ExpectedStatusCode}_With{CapitalizeFirst(sanitizedType)}";
    }

    private static string CapitalizeFirst(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];

    private static string GetTestValueForType(string type) => type.ToLowerInvariant() switch
    {
        "int" or "int32" or "int64" or "long" => "1",
        "guid" => "Guid.NewGuid()",
        "string" => "\"test\"",
        "bool" or "boolean" => "true",
        "datetime" or "dateonly" => "DateTime.UtcNow",
        "decimal" => "1.0m",
        "double" or "float" => "1.0",
        _ => "null"
    };
    private static string StatusCodeToHttpStatusCode(int statusCode) => statusCode switch
    {
        200 => "HttpStatusCode.OK",
        201 => "HttpStatusCode.Created",
        202 => "HttpStatusCode.Accepted",
        204 => "HttpStatusCode.NoContent",
        400 => "HttpStatusCode.BadRequest",
        404 => "HttpStatusCode.NotFound",
        409 => "HttpStatusCode.Conflict",
        _ => $"(HttpStatusCode){statusCode}"  // fallback for anything else
    };
    public static string BuildNotFoundTest(EndpointInfo endpoint, string url)
    {
        var sb = new StringBuilder();

        // Replace every route param value with a non-existent ID
        // e.g. "api/products/1" becomes "api/products/99999"
        var notFoundUrl = Regex.Replace(url, @"/\d+", "/99999");
        notFoundUrl = Regex.Replace(notFoundUrl, @"/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", $"/{Guid.NewGuid()}");

        sb.AppendLine("    [Fact]");
        sb.AppendLine($"    public async Task {endpoint.MethodName}_Returns404_WhenNotFound()");
        sb.AppendLine("    {");
        sb.AppendLine($"        var response = await _client.{GetHttpMethod(endpoint.HttpVerb)}Async(\"{notFoundUrl}\");");
        sb.AppendLine($"        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);");
        sb.AppendLine("    }");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string GetHttpMethod(string httpVerb) => httpVerb switch
    {
        "HttpGet" => "Get",
        "HttpDelete" => "Delete",
        "HttpPut" => "Put",
        _ => "Get"
    };
}