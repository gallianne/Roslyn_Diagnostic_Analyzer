using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CustomAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TargetTypedNewAnalyzer : DiagnosticAnalyzer
    {
        // Deux identifiants distincts pour les deux cas de figure
        public const string DIAGNOSTIC_ID_REQUIRE_NEW = "CLEAN003A";
        public const string DIAGNOSTIC_ID_FORBID_NEW = "CLEAN003B";

        private static readonly DiagnosticDescriptor RULE_REQUIRE_NEW = new DiagnosticDescriptor(
            DIAGNOSTIC_ID_REQUIRE_NEW,
            "prefer new()",
            "As the type is already defined on the same line, use “new()” to keep the code concise",
            "Readability",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor RULE_FORBID_NEW = new DiagnosticDescriptor(
            DIAGNOSTIC_ID_FORBID_NEW,
            "Forbidden new() usage",
            "The type is not declared on this line. Use an explicit instantiation (e.g. new MyClass()).",
            "Readability",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(RULE_REQUIRE_NEW, RULE_FORBID_NEW);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // S'abonne aux "new MyClass()"
            context.RegisterSyntaxNodeAction(AnalyzeExplicitNew, SyntaxKind.ObjectCreationExpression);

            // S'abonne aux "new()"
            context.RegisterSyntaxNodeAction(AnalyzeImplicitNew, SyntaxKind.ImplicitObjectCreationExpression);
        }

        private void AnalyzeExplicitNew(SyntaxNodeAnalysisContext context)
        {
            var explicitNew = (ObjectCreationExpressionSyntax)context.Node;

            // Si le type est sur la même ligne, "new MyClass()" lève un avertissement (il faut utiliser new())
            if (IsTypeDeclaredOnSameLine(explicitNew))
            {
                context.ReportDiagnostic(Diagnostic.Create(RULE_REQUIRE_NEW, explicitNew.GetLocation()));
            }
        }

        private void AnalyzeImplicitNew(SyntaxNodeAnalysisContext context)
        {
            var implicitNew = (ImplicitObjectCreationExpressionSyntax)context.Node;

            // Si le type N'EST PAS sur la même ligne, "new()" lève un avertissement (il faut utiliser new MyClass())
            if (!IsTypeDeclaredOnSameLine(implicitNew))
            {
                context.ReportDiagnostic(Diagnostic.Create(RULE_FORBID_NEW, implicitNew.GetLocation()));
            }
        }

        // Vérifie si le parent direct est une déclaration explicite
        private bool IsTypeDeclaredOnSameLine(ExpressionSyntax node)
        {
            var parent = node.Parent;

            // Une affectation initiale (avec le signe '=')
            if (parent is EqualsValueClauseSyntax equalsClause)
            {
                // Cas 1 : Déclaration d'une propriété (ex: public List<int> Ids { get; } = new... ;)
                if (equalsClause.Parent is PropertyDeclarationSyntax)
                {
                    return true;
                }

                // Cas 2 : Déclaration d'une variable locale ou d'un champ
                if (equalsClause.Parent is VariableDeclaratorSyntax declarator &&
                    declarator.Parent is VariableDeclarationSyntax declaration)
                {
                    // Si on utilise 'var', le type n'est techniquement pas défini à gauche, donc false.
                    // (Même si votre règle précédente l'interdit, il faut le prévoir)
                    if (declaration.Type is IdentifierNameSyntax id && id.Identifier.ValueText == "var")
                    {
                        return false;
                    }

                    // Déclaration explicite (ex: MyClass obj = new... ;)
                    return true;
                }
            }

            // Dans tous les autres cas (argument de méthode, affectation simple obj = new..., return),
            // le type n'est pas "défini" sur la même ligne.
            return false;
        }
    }
}