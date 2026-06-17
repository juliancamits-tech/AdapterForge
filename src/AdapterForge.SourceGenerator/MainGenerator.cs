using AdapterForge.SourceGenerator.AdapterForgeOperation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SourceGenerator.Provider;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace SourceGenerator
{
    /// <summary>
    /// Main source generator for CQRS.
    /// </summary>
    [Generator]
    public class MainGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// Initializes the source generator.
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //Debugger.Launch();
            var operations =
                context.SyntaxProvider
                    .CreateSyntaxProvider(
                        predicate: OperationDiscovery.IsCandidate,
                        transform: OperationDiscovery.Transform)
                    .Where(x => x is not null);


            var compilationAndDeclarations = context.CompilationProvider.Combine(operations.Collect());


            context.RegisterSourceOutput(compilationAndDeclarations,
                static (spc, source) => Execute(source.Left, source.Right!, spc));
        }

        private static void Execute(Compilation _, ImmutableArray<OperationDefinition> operationDefinitions, SourceProductionContext context)
        {
            HttpProvider.Cleaner();

            foreach (var operations in operationDefinitions.GroupBy(x => x.Group))
            {
                var auch = operations.ToImmutableArray();
                var result = HttpProvider.GenerateHttpFile(auch);
                SaveFile(result, $"Http{operations.Key}", "cs", context);

                result = McpProvider.GenerateMcpFile(auch);
                SaveFile(result, $"Mcp{operations.Key}", "cs", context);

                //result = gRpcProvider.GenerateProtoFile(auch);
                //SaveFile(result, $"{operations.Key}Grpc","proto", context);
            }

            SaveFile(HttpProvider.GenerateUnionFile(), "HttpAdapterForge", "cs", context);
        }

        private static bool SaveFile(string fileBody, string fileName, string extension, SourceProductionContext context)
        {
            if (string.IsNullOrEmpty(fileBody))
                return false;

            context.AddSource($"{fileName}.g.{extension}", SourceText.From(fileBody, Encoding.UTF8));
            return true;
        }
    }
}
