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
    public List<DependencyCallInfo> DependencyCalls { get; set; } = new();
    
    
}

public class ParameterDetail
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsFromBody { get; set; }
    public bool IsFromRoute { get; set; }
    public List<PropertyDetail> DtoProperties { get; set; } = new();
}

public class DependencyCallInfo
{
    public string DependencyName { get; set; } = string.Empty;  // e.g. productService
    public string DependencyType { get; set; } = string.Empty;  // e.g. IProductService
    public string MethodName { get; set; } = string.Empty;      // e.g. GetByIdAsync
    public List<string> ArgumentTypes { get; set; } = new();    // e.g. ["int", "string"]
    public string ReturnType { get; set; } = string.Empty;      // e.g. Product
    public bool IsAsync { get; set; }                           // true = ReturnsAsync
}