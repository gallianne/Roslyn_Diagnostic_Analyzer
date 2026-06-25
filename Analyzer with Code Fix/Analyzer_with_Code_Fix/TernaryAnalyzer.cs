using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Analyzer_with_Code_Fix
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TernaryAnalyzer : DiagnosticAnalyzer
    {
        // On sépare les diagnostics pour que le développeur comprenne exactement pourquoi son code est rejeté
        public const string DIAGNOSTIC_ID_NESTED = "CLEAN001A";
        public const string DIAGNOSTIC_ID_MULTI_LINE = "CLEAN001B";
        public const string DIAGNOSTIC_ID_TOO_LONG = "CLEAN001C";

        // Définissez ici la limite de caractères autorisée pour chaque partie du ternaire
        public const int MAX_EXPRESSION_LENGTH = 45;
        public const int MAX_LENGTH = 80;

        private static readonly DiagnosticDescriptor RULE_NESTED = new DiagnosticDescriptor(
            DIAGNOSTIC_ID_NESTED,
            "nested ternary expression",
            "The use of nested ternary operators is not permitted",
            "Readability", DiagnosticSeverity.Warning, true);

        private static readonly DiagnosticDescriptor RULE_MULTILINE = new DiagnosticDescriptor(
            DIAGNOSTIC_ID_MULTI_LINE,
            "Ternary operator across multiple lines",
            "A ternary operator must be written on a single line",
            "Readability", DiagnosticSeverity.Warning, true);

        private static readonly DiagnosticDescriptor RULE_TOO_LONG_COMPONANT = new DiagnosticDescriptor(
            DIAGNOSTIC_ID_TOO_LONG,
            "the ternary expression is too long",
            $"'{{0}}'  is over {MAX_EXPRESSION_LENGTH} characters",
            "Readability", DiagnosticSeverity.Warning, true);

        private static readonly DiagnosticDescriptor RULE_TOO_LONG_EXPRESSION = new DiagnosticDescriptor(
            DIAGNOSTIC_ID_TOO_LONG,
            "the ternary expression is too long",
            $"'{{0}}' is over {MAX_LENGTH} characters.",
            "Readability", DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(RULE_NESTED, RULE_MULTILINE, RULE_TOO_LONG_COMPONANT, RULE_TOO_LONG_EXPRESSION);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ConditionalExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var conditionalExpr = (ConditionalExpressionSyntax)context.Node;

            // 1. Vérification de l'imbrication
            bool isComplex = ContainsTernary(conditionalExpr.Condition) ||
                             ContainsTernary(conditionalExpr.WhenTrue) ||
                             ContainsTernary(conditionalExpr.WhenFalse);

            if (isComplex)
            {
                context.ReportDiagnostic(Diagnostic.Create(RULE_NESTED, conditionalExpr.GetLocation()));
            }

            // 2. Vérification des lignes multiples
            var lineSpan = conditionalExpr.SyntaxTree.GetLineSpan(conditionalExpr.Span);
            if (lineSpan.StartLinePosition.Line != lineSpan.EndLinePosition.Line)
            {
                context.ReportDiagnostic(Diagnostic.Create(RULE_MULTILINE, conditionalExpr.GetLocation()));
            }

            // 3. Vérification de la longueur de chaque composant
            CheckLength(context, conditionalExpr.Condition, RULE_TOO_LONG_COMPONANT, MAX_EXPRESSION_LENGTH);
            CheckLength(context, conditionalExpr.WhenTrue, RULE_TOO_LONG_COMPONANT, MAX_EXPRESSION_LENGTH);
            CheckLength(context, conditionalExpr.WhenFalse, RULE_TOO_LONG_COMPONANT, MAX_EXPRESSION_LENGTH);
            CheckLength(context, conditionalExpr, RULE_TOO_LONG_EXPRESSION, MAX_LENGTH);
        }

        // Fonction pour l'imbrication
        private static bool ContainsTernary(SyntaxNode node)
        {
            return node.DescendantNodesAndSelf()
                       .OfType<ConditionalExpressionSyntax>()
                       .Any();
        }

        // Fonction pour la limite de caractères
        private static void CheckLength(SyntaxNodeAnalysisContext context, ExpressionSyntax expression, DiagnosticDescriptor diagnosticDescriptor, int maxLength)
        {
            int length = expression.Span.Length;

            if (length > maxLength)
            {
                // Le message formatera {0}, {1} et {2} avec ces arguments
                Diagnostic diagnostic = Diagnostic.Create(diagnosticDescriptor, expression.GetLocation(), expression.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
