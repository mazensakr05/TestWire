using System.Text;
using System.Text.RegularExpressions;
using TestWire.cli.Analysis;

namespace TestWire.cli.Generation;

public static class MethodBodyBuilder
{


    public static string Build(EndpointInfo endpoint, string url , bool isNUnit = false)
    {
        var sb = new StringBuilder();
        var testName = BuildTestName(endpoint);
        var bodyParam = endpoint.Parameters.FirstOrDefault(p => p.IsFromBody);

        sb.AppendLine(isNUnit ? "    [Test]" : "    [Fact]");
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

    // ─────────────────────────────────────────────────────────────
    // METHOD 1: The public method that generates the full 404 test
    // ─────────────────────────────────────────────────────────────
    public static string BuildNotFoundTest(EndpointInfo endpoint, string baseRoute, string className, bool isNUnit = false)
    {
        var sb = new StringBuilder();

        // Step 1: Build the 404 URL using invalid values
        var notFoundUrl = BuildNotFoundUrl(endpoint, baseRoute, className);

        // Step 2: Framework-aware attribute — Bug #1 lesson applied here too
        sb.AppendLine(isNUnit ? "    [Test]" : "    [Fact]");
        sb.AppendLine($"    public async Task {endpoint.MethodName}_Returns404_WhenNotFound()");
        sb.AppendLine("    {");

        // Step 3: HTTP call — PUT/PATCH need a body (Bug #3 handled here)
        var httpCall = endpoint.HttpVerb switch
        {
            "HttpPut" => $"await _client.PutAsJsonAsync(\"{notFoundUrl}\", new {{ }});",
            "HttpPatch" => $"await _client.PatchAsJsonAsync(\"{notFoundUrl}\", new {{ }});",
            "HttpDelete" => $"await _client.DeleteAsync(\"{notFoundUrl}\");",
            _ => $"await _client.GetAsync(\"{notFoundUrl}\");"  // GET default
        };

        sb.AppendLine($"        var response = {httpCall}");

        // Step 4: Framework-aware assertion
        if (isNUnit)
            sb.AppendLine("        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));");
        else
            sb.AppendLine("        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);");

        sb.AppendLine("    }");
        sb.AppendLine();

        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────
    // METHOD 2: The private helper that builds the 404 URL safely
    // ─────────────────────────────────────────────────────────────
    private static string BuildNotFoundUrl(EndpointInfo endpoint, string baseRoute, string className)
    {
        // Start from the RAW route template — NOT the final built URL
        // e.g. endpoint.Route = "{id}"
        //      baseRoute      = "api/v1/[controller]"
        //      className      = "ProductsController"

        // Resolve [controller] token — same logic RouteBuilder uses
        const string suffix = "Controller";
        var controllerName = className.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? className[..^suffix.Length].ToLowerInvariant()
            : className.ToLowerInvariant();

        var resolvedBase = baseRoute.Replace("[controller]", controllerName, StringComparison.OrdinalIgnoreCase);

        // Combine base + endpoint route segment
        var combined = string.IsNullOrWhiteSpace(endpoint.Route)
            ? resolvedBase.Trim('/')
            : $"{resolvedBase.Trim('/')}/{endpoint.Route.Trim('/')}";

        // NOW replace {placeholders} with INVALID values based on type
        // This is surgical — only touches {id}, never touches "v1" or other literals
        var result = System.Text.RegularExpressions.Regex.Replace(
            combined,
            @"\{([^}]+)\}",   // matches {id}, {categoryId}, {id:int} etc.
            match =>
            {
                // Strip constraint: "{id:int}" → "id"
                var paramName = match.Groups[1].Value.Split(':')[0];

                // Find this param in the endpoint's parameter list
                var param = endpoint.Parameters.FirstOrDefault(p =>
                    p.IsFromRoute &&
                    p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));

                // Return an INVALID value based on the param's type
                return param?.Type.ToLowerInvariant() switch
                {
                    "guid" => Guid.NewGuid().ToString(), // random guid — guaranteed not in DB
                    "string" => "nonexistent-xyz-404",   // string that won't match any real record
                    _ => "99999"                          // int/long/anything else
                };
            });

        return result;
    }

    private static string GetHttpMethod(string httpVerb) => httpVerb switch
    {
        "HttpGet" => "Get",
        "HttpDelete" => "Delete",
        "HttpPut" => "Put",
        _ => "Get"
    };
}