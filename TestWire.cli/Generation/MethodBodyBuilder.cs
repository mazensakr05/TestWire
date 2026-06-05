using System.Runtime.Intrinsics.X86;
using System.Text;
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
        sb.AppendLine("        response.EnsureSuccessStatusCode();");

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
        var returnPart = string.IsNullOrEmpty(endpoint.ReturnType)
            ? ""
            : $"_With{CapitalizeFirst(endpoint.ReturnType.Replace("<", "Of").Replace(">", ""))}";

        return $"{endpoint.MethodName}_Returns200{returnPart}";
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
}