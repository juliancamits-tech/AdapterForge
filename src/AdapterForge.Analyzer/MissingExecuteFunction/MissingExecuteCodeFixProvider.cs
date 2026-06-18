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

namespace AdapterForge.Analyzer.MissingExecuteFunction
{
    [ExportCodeFixProvider(Const.Id.MissingExecuteId, LanguageNames.CSharp), Shared]
    public class MissingExecuteCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Const.Id.MissingExecuteId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagSpan = diagnostic.Location.SourceSpan;

            var classNode = root.FindToken(diagSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classNode is null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Agregar método Execute",
                    c => AddExecuteMethodAsync(context.Document, classNode, c),
                    equivalenceKey: "AddExecute"),
                diagnostic);
        }

        private async Task<Document> AddExecuteMethodAsync(Document document, ClassDeclarationSyntax classDecl, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl, cancellationToken);
            if (classSymbol is null)
                return document;

            // Find AdapterForgeOperation base generic args
            var current = classSymbol.BaseType;
            INamedTypeSymbol opBase = null;
            while (current != null)
            {
                if (current is INamedTypeSymbol named && named.IsGenericType && named.Name == "AdapterForgeOperation")
                {
                    opBase = named;
                    break;
                }
                current = current.BaseType;
            }

            if (opBase is null || opBase.TypeArguments.Length < 2)
                return document;

            var requestType = opBase.TypeArguments[0];
            var responseType = opBase.TypeArguments[1];

            // Build method syntax: public ResponseType Execute(RequestType request) { throw new NotImplementedException(); }
            var method = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(responseType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)), "Execute")
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("request")).WithType(SyntaxFactory.ParseTypeName(requestType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))
                )))
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.ThrowStatement(
                        SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("System.NotImplementedException")).WithArgumentList(SyntaxFactory.ArgumentList())
                    )
                ));

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var newClass = classDecl.AddMembers(method);
            var newRoot = root.ReplaceNode(classDecl, newClass);

            var newDoc = document.WithSyntaxRoot(newRoot);
            return newDoc;
        }
    }
}
