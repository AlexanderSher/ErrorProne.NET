﻿using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using ErrorProne.NET.Common;
using ErrorProne.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ErrorProne.NET.Rules.OtherRules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UseReadOnlyAttributeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly string Title = "Use ReadOnlyAttribute.";
        private static readonly string Message = "Use ReadOnlyAttribute.";
        // TODO: need perf proofs for this!
        private static readonly string Description = "ReadOnlyAttribute could improve overall performance and could be useful.";

        private const string Category = "Improvements";

        public static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(RuleIds.UseReadOnlyAttributeInstead, Title, Message, Category,
                DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
        }

        private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            var fieldDeclarations = (FieldDeclarationSyntax)context.Node;
            foreach (var fieldDeclaration in fieldDeclarations.Declaration.Variables)
            {
                var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(fieldDeclaration) as IFieldSymbol;

                Contract.Assert(fieldSymbol != null);

                if (fieldSymbol.HasReadOnlyAttribute()) continue;

                // TODO: technically, this is inverted predicate from similar method in ReadOnlyAttributeAnalyzer.cs!
                // Still not sure, whether it would be useful though
                if (fieldSymbol.IsReadOnly &&
                    !fieldSymbol.Type.IsReferenceType &&
                    !fieldSymbol.Type.IsEnum() && !fieldSymbol.Type.IsNullableEnum(context.SemanticModel) &&
                    !fieldSymbol.Type.IsPrimitiveType() && !fieldSymbol.Type.IsNullablePrimitiveType(context.SemanticModel))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(Rule, fieldDeclarations.GetLocation()));
                    break;
                }
            }
        }
    }
}