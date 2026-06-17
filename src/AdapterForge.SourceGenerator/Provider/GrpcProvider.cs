using AdapterForge.SourceGenerator.AdapterForgeOperation;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace SourceGenerator.Provider;

internal static class GrpcProvider
{
    public static string GenerateProtoFile(ImmutableArray<OperationDefinition> operationDefinitions)
    {
        var firstValidItem = operationDefinitions.FirstOrDefault(x => x.Grpc);
        if (firstValidItem is null)
            return string.Empty;

        var sb = new StringBuilder();

        sb.AppendLine("syntax = \"proto3\";");
        sb.AppendLine();

        sb.AppendLine($"package adapterforge.{firstValidItem.Group.ToLowerInvariant()};");
        sb.AppendLine();

        foreach (var item in operationDefinitions.Where(x => x.Grpc))
        {
            sb.AppendLine($"message {item.ResponseType}Message {{");
            var counter = 1;
            foreach (var prop in item.ResponseContract.Properties)
            {
                sb.AppendLine($"    string {prop.Name.ToLower()} = {counter}");
                counter++;
            }
            sb.AppendLine("}");
        }



        return sb.ToString();
    }
}
