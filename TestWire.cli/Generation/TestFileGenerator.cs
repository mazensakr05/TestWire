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
        sb.AppendLine(framework == "nunit" ? "using Nunit.Framework;" : "using Xunit");
        sb.AppendLine();
        
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

            var testName = $"{endpoint.MethodName}_ReturnsExpectedResult";
            sb.AppendLine($"    {testAttr}");
            sb.AppendLine($"    public async Task {testName}()");
            sb.AppendLine("    {");
            sb.AppendLine("        // Arrange");
            sb.AppendLine();
            sb.AppendLine("        // Act");
            sb.AppendLine();
            sb.AppendLine("        // Assert");
            sb.AppendLine("        throw new NotImplementedException();");
            sb.AppendLine("    }");
            sb.AppendLine();
            
        }
        sb.AppendLine("}");
        return sb.ToString();
    }
}