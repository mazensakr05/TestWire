using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestWire.cli.Analysis;

namespace TestWire.cli.Generation
{
    public static class RouteBuilder
    {
        public static string Build(string baseRoute, string className, string? routeSegment, List<ParameterDetail> parameters)
        {
            var controllerName = ResolveControllerName(className);
            var resolvedBase = baseRoute.Replace("[controller]", controllerName, StringComparison.OrdinalIgnoreCase);
            var combined = CombineSegments(resolvedBase, routeSegment);
            var routeWithTokens = ReplaceRouteTokens(combined, parameters);

            return AppendQueryParameters(routeWithTokens, parameters);
        }

        private static string AppendQueryParameters(string route, List<ParameterDetail> parameters)
        {
            var queryParams = parameters.Where(p => p.IsFromQuery).ToList();
            if (!queryParams.Any()) return route;

            var sb = new StringBuilder(route);
            sb.Append("?");
            for (int i = 0; i < queryParams.Count; i++)
            {
                var p = queryParams[i];
                sb.Append($"{p.Name}={GetTestValueForType(p.Type)}");
                if (i < queryParams.Count - 1) sb.Append("&");
            }

            return sb.ToString();
        }

        private static string ResolveControllerName(string className)
        ...
            const string suffix = "Controller";
            var name = className.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ? className[..^suffix.Length] : className;
            return name.ToLowerInvariant();
        }

        private static string CombineSegments(string baseRoute, string? routeSegment)
        {
            if (string.IsNullOrWhiteSpace(routeSegment)) return baseRoute.Trim('/');

            return $"{baseRoute.Trim('/')}/{routeSegment.Trim('/')}";
        }

        private static string ReplaceRouteTokens(string route, List<ParameterDetail> parameters)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                route,
                @"\{([^}]+)\}",
                match =>
                {
                    var token = match.Groups[1].Value.Split(':')[0].ToLowerInvariant();
                    return ResolveTokenValue(token, parameters);
                });
        }
        private static string ResolveTokenValue(string token, List<ParameterDetail> parameters)
        {
            var match = parameters.FirstOrDefault(p =>
                p.IsFromRoute &&
                p.Name.Equals(token, StringComparison.OrdinalIgnoreCase));

            // Real type from analyzer — no guessing
            if (match is not null)
                return GetTestValueForType(match.Type);

            // True fallback — should rarely hit this
            return "1";
        }

        private static string GetTestValueForType(string type) => type.ToLowerInvariant() switch
        {
            "int" or "int32" or "int64" or "long" => "1",
            "guid" => "00000000-0000-0000-0000-000000000001",
            "string" => "test",
            "bool" or "boolean" => "true",
            "datetime" or "dateonly" => "2024-01-01",
            "decimal" or "double" or "float" => "1",
            _ => "1"
        };
    }
}

