namespace TestWire.cli.Analysis;

public class EndpointInfo
{
    public string MethodName { get; set; } = string.Empty;
    public string HttpVerb { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public bool IsAsync { get; set; }
    public bool HasAuthorize { get; set; }

    public List<ParameterDetail> Parameters { get; set; } = new();
}

public class ParameterDetail
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsFromBody { get; set; }
    public bool IsFromRoute { get; set; }
    public List<PropertyDetail> DtoProperties { get; set; } = new();
}