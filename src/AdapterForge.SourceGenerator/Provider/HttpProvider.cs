using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace SourceGenerator.Provider
{
    internal static class HttpProvider
    {
        private static List<string> _serviceCollection;
        private static List<string> _mappings;

        public static void Cleaner()
        {
            _serviceCollection = [];
            _mappings = [];
        }

        public static string GenerateUnionFile()
        {
            if (_serviceCollection.Count is 0)
                return string.Empty;

            var sb = new StringBuilder();

            sb.AppendLine("using Microsoft.AspNetCore.Builder;");
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine();

            sb.AppendLine("namespace AdapterForge.Http.Generated;");
            sb.AppendLine();

            sb.AppendLine($"public static class ServiceCollectionExtensions");
            sb.AppendLine("{");

            sb.AppendLine($"    public static IServiceCollection AddAdapterForgeEndpoints(this IServiceCollection services)");
            sb.AppendLine("    {");
            foreach (var service in _serviceCollection)
            {
                sb.AppendLine($"        services.{service}();");
            }
            sb.AppendLine("        return services;");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine($"    public static IEndpointRouteBuilder MapAdapterForgeEndpoints(this IEndpointRouteBuilder app)");
            sb.AppendLine("    {");
            foreach (var mapping in _mappings)
            {
                sb.AppendLine($"        app.{mapping}();");
            }
            sb.AppendLine("        return app;");
            sb.AppendLine("    }");


            sb.AppendLine("}");
            return sb.ToString();
        }

        public static string GenerateHttpFile(ImmutableArray<OperationDefinition> operationDefinitions)
        {
            var firstValidItem = operationDefinitions.FirstOrDefault(x => x.Http != HttpMethod.NONE);
            if (firstValidItem is null)
                return string.Empty;


            var sb = new StringBuilder();

            sb.AppendLine("using Microsoft.AspNetCore.Builder;");
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");

            sb.AppendLine();

            sb.AppendLine("namespace AdapterForge.Http.Generated;");
            sb.AppendLine();

            sb.AppendLine($"public static class {firstValidItem.Group}Endpoints");
            sb.AppendLine("{");

            GenerateAddMethod(sb, firstValidItem.Group, operationDefinitions);

            sb.AppendLine();

            GenerateMapMethod(sb, firstValidItem.Group, operationDefinitions);

            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void GenerateAddMethod(StringBuilder sb, string group, ImmutableArray<OperationDefinition> operations)
        {
            var functionName = $"Add{group}Endpoints";
            _serviceCollection.Add(functionName);
            sb.AppendLine($"    public static IServiceCollection {functionName}(this IServiceCollection services)");
            sb.AppendLine("    {");

            foreach (var operation in operations)
            {
                sb.AppendLine($"        services.AddTransient<{operation.FullClassName}>();");
            }

            sb.AppendLine();
            sb.AppendLine("        return services;");
            sb.AppendLine("    }");
        }

        private static void GenerateMapMethod(StringBuilder sb, string group, ImmutableArray<OperationDefinition> operations)
        {
            var functionName = $"Map{group}Endpoints";
            _mappings.Add(functionName);
            sb.AppendLine($"    public static IEndpointRouteBuilder {functionName}(this IEndpointRouteBuilder app)");
            sb.AppendLine("    {");

            foreach (var operation in operations)
            {
                var sbFunction = new StringBuilder();
                var sbParams = new StringBuilder();
                foreach (var item in operation.ExecutionParams)
                {
                    sbFunction.Append(", ");
                    sbParams.Append(", ");
                    sbFunction.Append(item.Text);
                    sbParams.Append($"[FromServices] {item.Type} {item.Text}");
                }

                switch (operation.Http)
                {
                    case HttpMethod.POST:
                        sb.AppendLine($"        app.MapPost(\"/api/{group.ToLowerInvariant()}/{operation.ClassName.ToLower()}\",");
                        sb.AppendLine($"            ([FromBody] {operation.RequestType} request, [FromServices] {operation.FullClassName} operation{sbParams})");
                        sb.AppendLine($"                => operation.Execute(request{sbFunction}))");
                        break;
                    case HttpMethod.PUT:
                        sb.AppendLine($"        app.MapPut(\"/api/{group.ToLowerInvariant()}/{operation.ClassName.ToLower()}\",");
                        sb.AppendLine($"            ([FromBody] {operation.RequestType} request, [FromServices] {operation.FullClassName} operation{sbParams})");
                        sb.AppendLine($"                => operation.Execute(request{sbFunction}))");
                        break;
                    case HttpMethod.PATH:
                        sb.AppendLine($"        app.MapPath(\"/api/{group.ToLowerInvariant()}/{operation.ClassName.ToLower()}\",");
                        sb.AppendLine($"            ([FromBody] {operation.RequestType} request, [FromServices] {operation.FullClassName} operation{sbParams})");
                        sb.AppendLine($"                => operation.Execute(request{sbFunction}))");
                        break;
                    case HttpMethod.GET:
                        var requestDef = new StringBuilder();
                        var newObject = new StringBuilder();
                        foreach (var prop in operation.RequestContract.Properties)
                        {
                            var propNameQuery = prop.Name.ToLowerInvariant();
                            requestDef.Append($"[FromQuery] {prop.Type.FullName} {propNameQuery}, ");
                            newObject.Append($"{prop.Name} = {propNameQuery}, ");
                        }
                        sb.AppendLine($"        app.MapGet(\"/api/{group.ToLowerInvariant()}/{operation.ClassName.ToLower()}\",");
                        sb.AppendLine($"            ({requestDef}[FromServices] {operation.FullClassName} operation{sbParams})");
                        sb.AppendLine($"                => operation.Execute(new(){{{newObject}}}{sbFunction}))");
                        break;
                    case HttpMethod.DELETE:
                        sb.AppendLine($"        app.MapDelete(\"/api/{group.ToLowerInvariant()}/{operation.ClassName.ToLower()}\",");
                        break;
                }

                sb.AppendLine($"                   .WithDescription(\"{operation.Description}\")");
                sb.AppendLine("                        ;");
            }
            sb.AppendLine();


            sb.AppendLine("        return app;");
            sb.AppendLine("    }");
        }
    }
}
