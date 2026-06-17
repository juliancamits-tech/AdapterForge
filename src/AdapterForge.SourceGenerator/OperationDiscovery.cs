using AdapterForge.SourceGenerator.AdapterForgeOperation;
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
                            metadata.Mcp = GetMcpDefinition(invocation);
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

            private static McpDefinition GetMcpDefinition(InvocationExpressionSyntax invocation)
            {
                var isReadOnly = "false";
                var isDestructive = "true";
                var isIdempotent = "false";
                var openWorld = "false";
                for (int x = 0; x < invocation.ArgumentList.Arguments.Count; x++)
                {
                    var arg = invocation.ArgumentList.Arguments[x];
                    // Verificar si el argumento tiene nombre (named argument)
                    if (arg.NameColon != null)
                    {
                        // Es un argumento nombrado: authorization: "value"
                        var paramName = arg.NameColon.Name.Identifier.Text;
                        var paramValue = arg.Expression.ToString().ToLowerInvariant();

                        switch (paramName)
                        {
                            case "isReadOnly":
                                isReadOnly = paramValue;
                                break;
                            case "isDestructive":
                                isDestructive = paramValue;
                                break;
                            case "isIdempotent":
                                isIdempotent = paramValue;
                                break;
                            case "openWorld":
                                openWorld = paramValue;
                                break;
                        }
                    }
                    else
                    {
                        // Es un argumento posicional
                        var argValue = arg.ToString().ToLowerInvariant();

                        switch (x)
                        {
                            case 0:
                                isReadOnly = argValue;
                                break;
                            case 1:
                                isDestructive = argValue;
                                break;
                            case 2:
                                isIdempotent = argValue;
                                break;
                            case 3:
                                openWorld = argValue;
                                break;
                        }
                    }
                }

                var def = new McpDefinition();
                def.IsReadOnly = isReadOnly;
                def.IsDestructive = isDestructive;
                def.IsIdempotent = isIdempotent;
                def.OpenWorld = openWorld;

                return def;
            }

            private static HttpDefinition GetHttpMethod(InvocationExpressionSyntax invocation)
            {
                var arguments = invocation.ArgumentList.Arguments;

                // Variables para almacenar los valores
                string httpVerb = null;
                string authorization = null;
                string summary = null;

                foreach (var arg in arguments)
                {
                    // Verificar si el argumento tiene nombre (named argument)
                    if (arg.NameColon != null)
                    {
                        // Es un argumento nombrado: authorization: "value"
                        var paramName = arg.NameColon.Name.Identifier.Text;
                        var paramValue = arg.Expression.ToString().Trim('"');

                        switch (paramName)
                        {
                            case "httpMethod":
                                httpVerb = paramValue.Split('.').Last();
                                break;
                            case "authorization":
                                authorization = paramValue;
                                break;
                            case "summary":
                                summary = paramValue;
                                break;
                        }
                    }
                    else
                    {
                        // Es un argumento posicional
                        var argValue = arg.ToString();

                        // El primer argumento posicional es httpMethod (obligatorio)
                        if (httpVerb == null)
                        {
                            httpVerb = argValue.Split('.').Last();
                        }
                        // El segundo argumento posicional es authorization (opcional)
                        else if (authorization == null && !argValue.StartsWith("\""))
                        {
                            authorization = argValue.Trim('"');
                        }
                        // El tercer argumento posicional es summary (opcional)
                        else if (summary == null)
                        {
                            summary = argValue.Trim('"');
                        }
                    }
                }

                var def = new HttpDefinition();
                // Asignar el verbo HTTP
                if (httpVerb != null)
                {
                    switch (httpVerb)
                    {
                        case "GET":
                            def.Verb = HttpMethod.GET;
                            break;
                        case "POST":
                            def.Verb = HttpMethod.POST;
                            break;
                        case "PUT":
                            def.Verb = HttpMethod.PUT;
                            break;
                        case "DELETE":
                            def.Verb = HttpMethod.DELETE;
                            break;
                    }
                }

                // Guardar authorization y summary en HttpDefinition
                if (authorization is not null)
                    def.Authorization = authorization;

                def.Summary = summary;

                return def;
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
