using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AsyncSuggester
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AsyncSuggesterAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AsyncSuggester";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Performance";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var invocationExpr = (InvocationExpressionSyntax)context.Node;
            var memberAccessExpr = invocationExpr.Expression as MemberAccessExpressionSyntax;
            if (memberAccessExpr == null)
                return;
            
            // TODO: dunno why but that doesn't work
            //var methodSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpr).Symbol as IMethodSymbol;
            var methodSymbol = context.SemanticModel.GetSpeculativeSymbolInfo(context.Node.SpanStart, context.Node, SpeculativeBindingOption.BindAsExpression).Symbol as IMethodSymbol;
            if (methodSymbol == null || 
                methodSymbol.IsAsync || 
                IsTask(methodSymbol.ReturnType))
                return;

            if (HasTaskReturningMethod(methodSymbol.ReceiverType, methodSymbol.Name + "Async"))
                ReportDiagnostic(context, memberAccessExpr);

            if (methodSymbol.Name.EndsWith("Sync") && 
                HasTaskReturningMethod(methodSymbol.ReceiverType, methodSymbol.Name.Remove(methodSymbol.Name.Length - "Sync".Length)))
                ReportDiagnostic(context, memberAccessExpr);
        }

        static void ReportDiagnostic(SyntaxNodeAnalysisContext context, MemberAccessExpressionSyntax memberAccessExpr)
        {
            var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(), memberAccessExpr.Name);
            context.ReportDiagnostic(diagnostic);
        }

        static bool HasTaskReturningMethod(ITypeSymbol type, string name)
        {
            var members = type.GetMembers(name);
            foreach (var member in members)
            {
                var method = member as IMethodSymbol;
                if (method != null && IsTask(method.ReturnType))
                    return true;
            }
            return false;
        }

        static bool IsTask(ITypeSymbol symbol) => 
            symbol.ToString() == "System.Threading.Tasks.Task" ||
            symbol.ToString().StartsWith("System.Threading.Tasks.Task<");
    }
}
