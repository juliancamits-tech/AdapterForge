using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace SourceGenerator.Provider
{
    internal static class McpProvider
    {
        public static string GenerateMcpFile(ImmutableArray<OperationDefinition> operationDefinitions)
        {
            var firstValidItem = operationDefinitions.FirstOrDefault(x => x.Mcp);
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

            sb.AppendLine($"     [McpServerTool, Description(\"{operation.Description}\")]");
            sb.AppendLine($"    public static {operation.ResponseType} {operation.ClassName}(");
            sb.AppendLine($"{operation.RequestType} request, ");
            sb.AppendLine($"{operation.FullClassName} operation");
            sb.AppendLine($"{sbParams})");
            sb.AppendLine("     {");
            sb.AppendLine($"        return operation.Execute(request{sbFunction});");
            sb.AppendLine("     }");
        }
    }
}
