using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Motiv.CodeFix;

/// <summary>
/// Code fix provider that converts boolean expressions to use the Spec pattern to address Boolean Blindness.
/// It responds to diagnostics with ID "MOTIV0001" and replaces the identified expressions
/// with calls to the Spec.Build method, encapsulating the original expression.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MotivCodeFixProvider)), Shared]
public class MotivCodeFixProvider : CodeFixProvider
{
    /// <summary>
    /// Use literal ID string to avoid any potential type initialization issues during MEF discovery
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ["MOTIV0001"];

    /// <summary>
    /// Specifies the fix all provider, enabling batch fixes for all occurrences of the diagnostic.
    /// </summary>
    /// <returns>
    /// A <see cref="FixAllProvider"/> that can provide fixes for all occurrences of the current code issue.
    /// </returns>
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <summary>
    /// Registers code fixes for the diagnostics found in the code.
    /// It identifies boolean expressions and offers to convert them into logical propositions using the Spec pattern.
    /// </summary>
    /// <param name="context">
    /// The context in which the code fix is being registered, including the document, diagnostics, and cancellation token.
    /// </param>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        try
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null) return;

            var logicalExpressionConverter = new LogicalExpressionToSpecConverter("Proposition", "Model", context.Document);

            foreach (var diagnostic in context.Diagnostics)
            {
                // Ensure we only attempt to fix when the node is an expression
                var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                if (node is not ExpressionSyntax expressionSyntax)
                    continue;

                var action = CodeAction.Create(
                    title: "Fix Boolean Blindness",
                    createChangedDocument: cancellationToken => logicalExpressionConverter.Convert(diagnostic, expressionSyntax, cancellationToken),
                    equivalenceKey: "ConvertToSpec");

                context.RegisterCodeFix(action, diagnostic);
            }

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Code fix error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}
