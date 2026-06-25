using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MaxParametersAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MethodParameterCountAnalyzer : DiagnosticAnalyzer
    {
        // Define a unique ID for your rule
        public const string DIAGNOSTIC_ID = "CLEAN004";

        // Metadata for the rule (appears in the IDE)
        private static readonly LocalizableString TITLE = "Method has too many parameters";
        private static readonly LocalizableString MESSAGE_FORMAT = "Method '{0}' has {1} parameters. A maximum of 3 is allowed.";
        private static readonly LocalizableString DESCRIPTION = "Methods with more than 3 parameters are hard to read and maintain. Consider refactoring to use a class/struct to group parameters.";
        private const string CATEGORY = "Readability";

        // Define the rule itself. Set DiagnosticSeverity.Error if you want to strictly forbid it and break the build.
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DIAGNOSTIC_ID,
            TITLE,
            MESSAGE_FORMAT,
            CATEGORY,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: DESCRIPTION);

        // Register the rule
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            // Boilerplate initialization
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register an action to trigger every time the compiler sees a Method Declaration
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            // Cast the current node to a MethodDeclarationSyntax
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            // Count the parameters
            int parameterCount = methodDeclaration.ParameterList.Parameters.Count;

            // Check against our rule limit (3)
            if (parameterCount > 3)
            {
                // Create the diagnostic (the red squiggle) pointing to the method's name
                var diagnostic = Diagnostic.Create(
                    Rule,
                    methodDeclaration.Identifier.GetLocation(),
                    methodDeclaration.Identifier.Text,
                    parameterCount);

                // Report it to Visual Studio / the compiler
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}