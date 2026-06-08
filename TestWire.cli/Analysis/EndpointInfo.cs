namespace TestWire.cli.Analysis;

public enum ReturnTypeKind
{
    Unknown,
    ActionResultOfT,
    IActionResultWithInferredT,
    PlainType

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

    public int ExpectedStatusCode { get; set; } = 200;

    public List<ParameterDetail> Parameters { get; set; } = new();

    public List<ProducesResponseDetail> ProducesResponses { get; set; } = new();


}

public class ParameterDetail
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string FullyQualifiedType { get; set; } = string.Empty;
    public bool IsFromBody { get; set; }
    public bool IsFromRoute { get; set; }
    public bool IsFromQuery { get; set; }
    public bool IsFromHeader { get; set; }

    public List<PropertyDetail> DtoProperties { get; set; } = new();

}

public class ProducesResponseDetail
{
    public int StatusCode { get; set; }
    public string? TypeName { get; set; }
}