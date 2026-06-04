namespace TestWire.cli.Analysis;

public enum ReturnTypeKind {
Unknown ,
ActionResultOfT,
IActionResultWithInferredT

}

public class EndpointInfo
{
    public string MethodName { get; set; } = string.Empty;
    public string HttpVerb { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public ReturnTypeKind ReturnTypeKind { get; set; } = ReturnTypeKind.Unknown;
    public bool HasAmbiguousReturnType { get; set; }

    public bool IsAsync { get; set; }
    public bool HasAuthorize { get; set; }
    public bool HasAllowAnonymous { get; set; }

    public List<ParameterDetail> Parameters { get; set; } = new();


}

public class ParameterDetail
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsFromBody { get; set; }
    public bool IsFromRoute { get; set; }
    public bool IsFromQuery { get; set; }

    public List<PropertyDetail> DtoProperties { get; set; } = new();

}