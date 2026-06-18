using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AdapterForge.Analyzer.ExecuteFirstParam
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExecuteFirstParamAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString Title = "El primer parámetro de Execute debe ser el tipo de petición";
        private static readonly LocalizableString MessageFormat = "El método 'Execute' debe recibir como primer parámetro '{0}' (TRequest)";
        private static readonly LocalizableString Description = "En las clases que heredan de AdapterForgeOperation<TRequest,TResponse> el método Execute debe aceptar el TRequest como primer parámetro.";

        private static readonly DiagnosticDescriptor Rule = new(
            Const.Id.ExecutionTRequestFirstId,
            Title,
            MessageFormat,
            Const.Category.Error,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var methodDecl = (MethodDeclarationSyntax)context.Node;

            if (methodDecl.Identifier.Text != "Execute")
                return;

            if (methodDecl.Parent is not ClassDeclarationSyntax classDecl)
                return;

            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
            if (classSymbol is null)
                return;

            // find AdapterForgeOperation<TRequest, TResponse> in base types
            var current = classSymbol.BaseType;
            INamedTypeSymbol opBase = null;
            while (current != null)
            {
                if (current is INamedTypeSymbol named && named.IsGenericType && named.Name == "AdapterForgeOperation")
                {
                    if (named.ContainingNamespace != null && named.ContainingNamespace.ToDisplayString() == "AdapterForge.Abstractions")
                    {
                        opBase = named;
                        break;
                    }
                }
                current = current.BaseType;
            }

            if (opBase is null || opBase.TypeArguments.Length < 1)
                return;

            var requestType = opBase.TypeArguments[0];

            var parameters = methodDecl.ParameterList.Parameters;
            if (parameters.Count == 0)
                return; // other analyzer handles missing Execute

            // get symbol for first parameter
            var firstParam = parameters[0];
            var firstParamType = context.SemanticModel.GetTypeInfo(firstParam.Type!, context.CancellationToken).Type;

            if (SymbolEqualityComparer.Default.Equals(firstParamType, requestType))
                return; // OK

            // check if any other parameter has requestType
            bool hasRequestParamElsewhere = false;
            foreach (var p in parameters)
            {
                var t = context.SemanticModel.GetTypeInfo(p.Type!, context.CancellationToken).Type;
                if (SymbolEqualityComparer.Default.Equals(t, requestType))
                {
                    hasRequestParamElsewhere = true;
                    break;
                }
            }

            if (!hasRequestParamElsewhere)
                return; // nothing to reorder (maybe missing), skip

            // report diagnostic on the method identifier
            var diag = Diagnostic.Create(Rule, methodDecl.Identifier.GetLocation(), requestType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            context.ReportDiagnostic(diag);
        }
    }
}
