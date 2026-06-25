using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CustomAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NoVarAnalyzer : DiagnosticAnalyzer
    {
        public const string DIAGNOSTIC_ID = "CLEAN002";

        private static readonly LocalizableString TITLE = "Forbidden 'var' usage";
        private static readonly LocalizableString MESSAGE_FORMAT = "The use of 'var' is not permitted here. Declare the type explicitly.";
        private static readonly LocalizableString DESCRIPTION = "Enforces the use of explicit types, except in foreach loops.";
        private const string CATEGORY = "Readability";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DIAGNOSTIC_ID,
            TITLE,
            MESSAGE_FORMAT,
            CATEGORY,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: DESCRIPTION);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // 1. Cible les déclarations de variables classiques (ex: var x = 5; ou using(var y = ...))
            context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);

            // 2. Cible l'utilisation de var dans le pattern matching (ex: if (x is var y))
            context.RegisterSyntaxNodeAction(AnalyzeVarPattern, SyntaxKind.VarPattern);
        }

        private void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            var declaration = (VariableDeclarationSyntax)context.Node;

            // On vérifie si le type écrit est exactement "var"
            if (declaration.Type is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.ValueText == "var")
            {
                var diagnostic = Diagnostic.Create(Rule, declaration.Type.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeVarPattern(SyntaxNodeAnalysisContext context)
        {
            var pattern = (VarPatternSyntax)context.Node;

            // Lève une erreur directement sur le mot-clé 'var' du pattern matching
            var diagnostic = Diagnostic.Create(Rule, pattern.VarKeyword.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}