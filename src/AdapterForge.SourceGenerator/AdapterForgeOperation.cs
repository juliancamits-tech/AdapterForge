using System.Collections.Generic;

namespace SourceGenerator;

internal class OperationDefinition
{
    public string Namespace { get; set; }

    public string ClassName { get; set; }
    public string FullClassName => $"{Namespace}.{ClassName}";

    public string RequestType { get; set; }

    public string ResponseType { get; set; }

    public string Group { get; set; }

    public string Description { get; set; }

    public List<OperationExecutionParam> ExecutionParams { get; set; } = [];

    public HttpMethod Http { get; set; } = HttpMethod.NONE;

    public bool Mcp { get; set; }

    public bool Grpc { get; set; }
    public BuildContractTypeDefinition RequestContract { get; internal set; }
    public BuildContractTypeDefinition ResponseContract { get; internal set; }
}

internal class OperationExecutionParam(string type, string text)
{
    public string Type { get; set; } = type;
    public string Text { get; set; } = text;
}

internal class BuildContractTypeDefinition
{
    public string Name { get; set; }

    public string FullName { get; set; }

    public ContractTypeKind Kind { get; set; }

    public bool Nullable { get; set; }
    public bool IsPrimitive { get; set; }

    public List<ContractPropertyDefinition> Properties { get; set; } = [];
}

internal enum ContractTypeKind
{
    Primitive,
    Complex,
    Enum,
    Collection
}

internal class ContractPropertyDefinition
{
    public string Name { get; set; }

    public BuildContractTypeDefinition Type { get; set; }
}