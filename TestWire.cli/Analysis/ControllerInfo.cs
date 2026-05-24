namespace TestWire.cli.Analysis;

public class ControllerInfo
{
    public string ClassName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string BaseRoute { get; set; } = string.Empty;
    public List<EndpointInfo> Endpoints { get; set; } = new();
    public List<ConstructorDependency> Dependencies { get; set; } = new();
}