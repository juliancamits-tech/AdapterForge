using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace AdapterForge.Analyzer.MissingExecuteFunction
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MissingExecuteAnalyzer : DiagnosticAnalyzer
    {        
        private static readonly LocalizableString Title = "Falta método Execute";
        private static readonly LocalizableString MessageFormat = "La clase '{0}' hereda de AdapterForgeOperation y debe declarar un método 'Execute'";
        private static readonly LocalizableString Description = "Las operaciones que heredan de AdapterForgeOperation deben exponer un método Execute(TRequest) que devuelva TResponse.";

        private static readonly DiagnosticDescriptor Rule = new(
            Const.Id.MissingExecuteId,
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

            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;

            var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
            if (symbol is null)
                return;

            // Check base types chain for AdapterForgeOperation<,>
            var current = symbol.BaseType;
            bool isOperation = false;

            while (current != null)
            {
                if (current is INamedTypeSymbol named && named.IsGenericType && named.Name == "AdapterForgeOperation")
                {
                    // Check namespace
                    if (named.ContainingNamespace != null && named.ContainingNamespace.ToDisplayString() == "YAAF.Abstractions")
                    {
                        isOperation = true;
                        break;
                    }
                }

                current = current.BaseType;
            }

            if (!isOperation)
                return;

            // Has an Execute member?
            foreach (var member in symbol.GetMembers())
            {
                if (member is IMethodSymbol ms && ms.Name == "Execute")
                    return; // found
            }

            // Report diagnostic on class identifier
            var diag = Diagnostic.Create(Rule, classDecl.Identifier.GetLocation(), symbol.Name);
            context.ReportDiagnostic(diag);
        }
    }
}
