namespace TestWire.cli.Analysis;

public enum ReturnTypeKind
{
    Unknown,
    ActionResultOfT,
    IActionResultWithInferredT,
    PlainType

}

public sealed record EndpointInfo(
    string MethodName,
    string HttpVerb,
    string Route,
    string ReturnType,
    ReturnTypeKind ReturnTypeKind,
    bool HasAmbiguousReturnType,
    bool IsAsync,
    bool HasAuthorize,
    bool HasAllowAnonymous,
    int ExpectedStatusCode,
    List<ParameterDetail> Parameters,
    List<ProducesResponseDetail> ProducesResponses
);

public sealed record ParameterDetail(
    string Name,
    string Type,
    string FullyQualifiedType,
    bool IsFromBody,
    bool IsFromRoute,
    bool IsFromQuery,
    bool IsFromHeader,
    List<PropertyDetail> DtoProperties
);

<<<<<<< Updated upstream
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

    public List<PropertyDetail> DtoProperties { get; set; } = new();

}

public class ProducesResponseDetail
{
    public int StatusCode { get; set; }
    public string? TypeName { get; set; }
}
=======
public sealed record ProducesResponseDetail(
    int StatusCode,
    string? TypeName
);
>>>>>>> Stashed changes
