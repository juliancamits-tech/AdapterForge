#nullable enable

using System.Collections.Generic;

namespace AdapterForge.SourceGenerator.AdapterForgeOperation
{
    internal class OperationDefinition
    {
        public string Namespace { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;
        public string FullClassName => $"{Namespace}.{ClassName}";

        public string RequestType { get; set; } = string.Empty;

        public string ResponseType { get; set; } = string.Empty;

        public string Group { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<OperationExecutionParam> ExecutionParams { get; set; } = [];

        public HttpDefinition? Http { get; set; } = null;

        public McpDefinition? Mcp { get; set; } = null;

        public bool Grpc { get; set; }
        public BuildContractTypeDefinition? RequestContract { get; internal set; }
        public BuildContractTypeDefinition? ResponseContract { get; internal set; }
    }
}
