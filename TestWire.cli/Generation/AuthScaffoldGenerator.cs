using System.Text;

namespace TestWire.cli.Generation;

public static class AuthScaffoldGenerator
{
    public static string GenerateTestAuthHandler(string projectNamespace)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System.Security.Claims;");
        sb.AppendLine("using System.Text.Encodings.Web;");
        sb.AppendLine("using Microsoft.AspNetCore.Authentication;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine("using Microsoft.Extensions.Options;");
        sb.AppendLine();
        sb.AppendLine($"namespace {projectNamespace}.Tests;");
        sb.AppendLine();
        sb.AppendLine("public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>");
        sb.AppendLine("{");
        sb.AppendLine("    public TestAuthHandler(");
        sb.AppendLine("        IOptionsMonitor<AuthenticationSchemeOptions> options,");
        sb.AppendLine("        ILoggerFactory logger,");
        sb.AppendLine("        UrlEncoder encoder)");
        sb.AppendLine("        : base(options, logger, encoder)");
        sb.AppendLine("    {");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    protected override Task<AuthenticateResult> HandleAuthenticateAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        var claims = new[]");
        sb.AppendLine("        {");
        sb.AppendLine("            new Claim(ClaimTypes.Name, \"testwire-user\"),");
        sb.AppendLine("            new Claim(ClaimTypes.NameIdentifier, \"1\"),");
        sb.AppendLine("            new Claim(ClaimTypes.Role, \"Admin\"),");
        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        var identity  = new ClaimsIdentity(claims, \"Bearer\");");
        sb.AppendLine("        var principal = new ClaimsPrincipal(identity);");
        sb.AppendLine("        var ticket    = new AuthenticationTicket(principal, \"Bearer\");");
        sb.AppendLine();
        sb.AppendLine("        return Task.FromResult(AuthenticateResult.Success(ticket));");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public static string GenerateCustomWebApplicationFactory(string projectNamespace)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Microsoft.AspNetCore.Authentication;");
        sb.AppendLine("using Microsoft.AspNetCore.Hosting;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc.Testing;");
        sb.AppendLine("using Microsoft.AspNetCore.TestHost;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine($"using {projectNamespace};");
        sb.AppendLine();
        sb.AppendLine($"namespace {projectNamespace}.Tests;");
        sb.AppendLine();
        sb.AppendLine("public class CustomWebApplicationFactory : WebApplicationFactory<Program>");
        sb.AppendLine("{");
        sb.AppendLine("    protected override void ConfigureWebHost(IWebHostBuilder builder)");
        sb.AppendLine("    {");
        sb.AppendLine("        builder.ConfigureTestServices(services =>");
        sb.AppendLine("        {");
        sb.AppendLine("            services.AddAuthentication(options =>");
        sb.AppendLine("            {");
        sb.AppendLine("                options.DefaultAuthenticateScheme = \"Bearer\";");
        sb.AppendLine("                options.DefaultChallengeScheme = \"Bearer\";");
        sb.AppendLine("            })");
        sb.AppendLine("            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(\"Bearer\", _ => { });");
        sb.AppendLine("        });");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}