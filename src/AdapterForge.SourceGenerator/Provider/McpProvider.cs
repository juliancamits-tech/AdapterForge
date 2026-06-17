using AdapterForge.SourceGenerator.AdapterForgeOperation;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace SourceGenerator.Provider
{
    internal static class McpProvider
    {
        public static string GenerateMcpFile(ImmutableArray<OperationDefinition> operationDefinitions)
        {
            var firstValidItem = operationDefinitions.FirstOrDefault(x => x.Mcp is not null);
            if (firstValidItem is null)
                return string.Empty;

            var sb = new StringBuilder();

            sb.AppendLine("using ModelContextProtocol.Server;");
            sb.AppendLine("using System.ComponentModel;");

            sb.AppendLine();

            sb.AppendLine("namespace AdapterForge.Http.Generated;");
            sb.AppendLine();

            sb.AppendLine("[McpServerToolType]");
            sb.AppendLine($"public static class {firstValidItem.Group}Mcp");
            sb.AppendLine("{");

            foreach (var op in operationDefinitions)
                CreateFunction(op, sb);

            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void CreateFunction(OperationDefinition operation, StringBuilder sb)
        {
            var sbFunction = new StringBuilder();
            var sbParams = new StringBuilder();
            for (int x = 0; x < operation.ExecutionParams.Count; x++)
            {
                var item = operation.ExecutionParams[x];
                sbFunction.Append(", ");
                sbParams.Append(", ");
                sbFunction.Append(item.Text);
                sbParams.Append($"{item.Type} {item.Text}");
            }

            sb.AppendLine($"    [McpServerTool");
            sb.AppendLine($"    (");
            sb.AppendLine($"    ReadOnly = {operation.Mcp.IsReadOnly},");
            sb.AppendLine($"    Destructive = {operation.Mcp.IsDestructive},");
            sb.AppendLine($"    Idempotent = {operation.Mcp.IsIdempotent},");
            sb.AppendLine($"    OpenWorld = {operation.Mcp.OpenWorld},");
            sb.AppendLine($"    OutputSchemaType = typeof({operation.ResponseType})");
            sb.AppendLine("     )]");
            sb.AppendLine($"    [Description(\"{operation.Description}\")]");
            sb.AppendLine($"    public static {operation.ResponseType} {operation.ClassName}({operation.RequestType} request, {operation.FullClassName} operation{sbParams})");
            sb.AppendLine("     {");
            sb.AppendLine($"        return operation.Execute(request{sbFunction});");
            sb.AppendLine("     }");
        }
    }
}
