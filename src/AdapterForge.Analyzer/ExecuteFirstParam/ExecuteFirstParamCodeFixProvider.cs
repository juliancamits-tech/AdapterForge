using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdapterForge.Analyzer.ExecuteFirstParam
{
    [ExportCodeFixProvider(Const.Id.ExecutionTRequestFirstId, LanguageNames.CSharp), Shared]
    public class ExecuteFirstParamCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Const.Id.ExecutionTRequestFirstId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagSpan = diagnostic.Location.SourceSpan;

            var methodNode = root.FindToken(diagSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (methodNode is null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Mover TRequest como primer parámetro",
                    c => MoveRequestParamFirstAsync(context.Document, methodNode, c),
                    equivalenceKey: "MoveRequestFirst"),
                diagnostic);
        }

        private async Task<Document> MoveRequestParamFirstAsync(Document document, MethodDeclarationSyntax methodDecl, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (methodDecl.Parent is not ClassDeclarationSyntax classDecl)
                return document;

            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl, cancellationToken);
            if (classSymbol is null)
                return document;

            // find AdapterForgeOperation base generic args
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
                return document;

            var requestType = opBase.TypeArguments[0];

            var parameters = methodDecl.ParameterList.Parameters;

            int requestIndex = -1;
            for (int i = 0; i < parameters.Count; i++)
            {
                var p = parameters[i];
                var t = semanticModel.GetTypeInfo(p.Type!, cancellationToken).Type;
                if (SymbolEqualityComparer.Default.Equals(t, requestType))
                {
                    requestIndex = i;
                    break;
                }
            }

            if (requestIndex <= 0)
                return document; // already first or not found

            var requestParam = parameters[requestIndex];

            var newParams = parameters.RemoveAt(requestIndex).Insert(0, requestParam);

            var newMethod = methodDecl.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(newParams)));

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(methodDecl, newMethod);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
