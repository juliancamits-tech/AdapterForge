using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading;

namespace SourceGenerator
{
    internal static class OperationDiscovery
    {
        public static bool IsCandidate(SyntaxNode node, CancellationToken _)
        {
            return node is ClassDeclarationSyntax;
        }

        public static OperationDefinition Transform(GeneratorSyntaxContext context, CancellationToken _)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;

            if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol symbol)
                return null;

            if (!IsOperation(symbol))
                return null;

            var baseType = symbol.BaseType!;

            var requestType =
                baseType.TypeArguments[0];

            var responseType =
                baseType.TypeArguments[1];

            var metadata = OperationMetadataParser.Parse(classDeclaration);
            metadata.Namespace = symbol.ContainingNamespace.ToDisplayString();
            metadata.ClassName = symbol.Name;
            metadata.RequestType = requestType.ToDisplayString();
            metadata.ResponseType = responseType.ToDisplayString();
            metadata.RequestContract = BuildContractTypeDefinition(requestType);
            metadata.ResponseContract = BuildContractTypeDefinition(responseType);

            var executeMethod = classDeclaration.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(x => x.Identifier.Text == "Execute");

            if (executeMethod is not null)
            {
                var methodSymbol = context.SemanticModel.GetDeclaredSymbol(executeMethod);

                if (methodSymbol is null)
                    return null;

                if (methodSymbol is not IMethodSymbol ms)
                    return null;

                foreach (var parameter in ms.Parameters)
                {
                    var type = parameter.Type.ToDisplayString();

                    var name = parameter.Name;

                    if (string.Compare(metadata.RequestType, type) == 0)
                        continue;

                    metadata.ExecutionParams.Add(new OperationExecutionParam(type, name));
                }
            }

            return metadata;
        }

        private static bool IsOperation(INamedTypeSymbol symbol)
        {
            var current = symbol;

            while (current is not null)
            {
                var baseType = current.BaseType;

                if (baseType is null)
                    return false;

                if (baseType.Name == "AdapterForgeOperation")
                    return true;

                current = baseType;
            }

            return false;
        }

        internal static class OperationMetadataParser
        {
            public static OperationDefinition Parse(
                ClassDeclarationSyntax classNode)
            {
                var metadata = new OperationDefinition();

                var configureMethod = classNode.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(x => x.Identifier.Text == "Configure");

                if (configureMethod is null)
                    return metadata;

                var invocations = configureMethod.DescendantNodes().OfType<InvocationExpressionSyntax>();

                foreach (var invocation in invocations)
                {
                    if (invocation.Expression is not MemberAccessExpressionSyntax member)
                    {
                        continue;
                    }

                    var methodName = member.Name.Identifier.Text;

                    switch (methodName)
                    {
                        case "Http":
                            metadata.Http = GetHttpMethod(invocation);
                            break;

                        case "Mcp":
                            metadata.Mcp = true;
                            break;

                        case "Grpc":
                            metadata.Grpc = true;
                            break;

                        case "Group":
                            metadata.Group = GetStringArgument(invocation);
                            break;

                        case "Description":
                            metadata.Description = GetStringArgument(invocation);
                            break;
                    }
                }

                return metadata;
            }

            private static string GetStringArgument(InvocationExpressionSyntax invocation)
            {
                return invocation.ArgumentList
                    .Arguments
                    .FirstOrDefault()?
                    .Expression
                    .ToString()
                    .Trim('"');
            }

            private static HttpMethod GetHttpMethod(InvocationExpressionSyntax invocation)
            {
                var verb = invocation.ArgumentList.Arguments.FirstOrDefault().ToString().Split('.').Last();

                return verb switch
                {
                    "GET" => HttpMethod.GET,
                    "POST" => HttpMethod.POST,
                    "PUT" => HttpMethod.PUT,
                    "DELETE" => HttpMethod.DELETE,
                    _ => HttpMethod.NONE,
                };
            }
        }

        private static BuildContractTypeDefinition BuildContractTypeDefinition(ITypeSymbol symbol)
        {
            var result = new BuildContractTypeDefinition
            {
                Name = symbol.Name,
                FullName = symbol.ToDisplayString()
            };

            if (IsPrimitive(symbol))
            {
                result.IsPrimitive = true;
                return result;
            }

            if (symbol is not INamedTypeSymbol namedType)
                return result;

            foreach (var property in namedType.GetMembers().OfType<IPropertySymbol>())
            {
                result.Properties.Add(
                    new ContractPropertyDefinition
                    {
                        Name = property.Name,
                        Type = BuildContractTypeDefinition(property.Type)
                    });
            }

            return result;
        }

        private static bool IsPrimitive(ITypeSymbol symbol)
        {
            return symbol.SpecialType switch
            {
                SpecialType.System_Boolean => true,
                SpecialType.System_Byte => true,
                SpecialType.System_SByte => true,
                SpecialType.System_Int16 => true,
                SpecialType.System_UInt16 => true,
                SpecialType.System_Int32 => true,
                SpecialType.System_UInt32 => true,
                SpecialType.System_Int64 => true,
                SpecialType.System_UInt64 => true,
                SpecialType.System_Single => true,
                SpecialType.System_Double => true,
                SpecialType.System_Decimal => true,
                SpecialType.System_Char => true,
                SpecialType.System_String => true,
                _ => false
            };
        }
    }
}
