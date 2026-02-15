using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Tenekon.CommandLine.Extensions.PolyType.SourceGenerator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommandSpecDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableArray<DiagnosticDescriptor> Supported =
    [
        GeneratorDiagnostics.TypeMustBePartial,
        GeneratorDiagnostics.InvalidType
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Supported;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(startContext =>
        {
            if (IsAttributesOnly(startContext.Options.AnalyzerConfigOptionsProvider)) return;

            var targets = new ConcurrentBag<INamedTypeSymbol>();

            startContext.RegisterOperationAction(
                operationContext => AnalyzeAttributeOperation(operationContext, targets),
                OperationKind.Attribute);

            startContext.RegisterCompilationEndAction(endContext =>
            {
                if (targets.IsEmpty) return;

                foreach (var symbol in targets.Distinct(new NamedTypeSymbolComparer()))
                {
                    var isDelegate = symbol.TypeKind == TypeKind.Delegate;

                    if (!isDelegate && !IsPartial(symbol))
                    {
                        endContext.ReportDiagnostic(
                            Diagnostic.Create(
                                GeneratorDiagnostics.TypeMustBePartial,
                                symbol.Locations[index: 0],
                                symbol.Name));
                        continue;
                    }

                    if (!isDelegate && (symbol.TypeKind != TypeKind.Class || symbol.IsAbstract || symbol.IsGenericType
                            || symbol.IsStatic))
                        endContext.ReportDiagnostic(
                            Diagnostic.Create(
                                GeneratorDiagnostics.InvalidType,
                                symbol.Locations[index: 0],
                                symbol.Name));
                }
            });

            return;

            void AnalyzeAttributeOperation(
                OperationAnalysisContext operationContext,
                ConcurrentBag<INamedTypeSymbol> collectedTargets)
            {
                if (operationContext.Operation is not IAttributeOperation attributeOperation) return;

                var attributeClass = GetAttributeClass(attributeOperation);
                if (attributeClass is null) return;

                if (!IsTargetAttribute(attributeClass, AttributeNames.CommandSpecAttribute)) return;
                if (operationContext.ContainingSymbol is not INamedTypeSymbol typeSymbol) return;

                collectedTargets.Add(typeSymbol);
            }

            static bool IsTargetAttribute(INamedTypeSymbol attributeClass, string expectedFullName)
            {
                var display = attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (display.StartsWith("global::", StringComparison.Ordinal))
                    display = display.Substring("global::".Length);

                return string.Equals(display, expectedFullName, StringComparison.Ordinal);
            }

            static INamedTypeSymbol? GetAttributeClass(IAttributeOperation attributeOperation)
            {
                var operation = attributeOperation.Operation;

                return operation switch
                {
                    IObjectCreationOperation creation => creation.Constructor?.ContainingType
                        ?? creation.Type as INamedTypeSymbol,
                    IInvalidOperation invalid => invalid.Type as INamedTypeSymbol,
                    _ => operation.Type as INamedTypeSymbol
                };
            }

            static bool IsAttributesOnly(AnalyzerConfigOptionsProvider optionsProvider)
            {
                if (optionsProvider.GlobalOptions.TryGetValue(
                        "build_property.TenekonCommandLineExtensionsPolyTypeSourceGeneratorAttributesOnly",
                        out var raw))
                    return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(raw, "1", StringComparison.Ordinal);

                return false;
            }
        });
    }

    private static bool IsPartial(INamedTypeSymbol symbol)
    {
        foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
            if (syntaxRef.GetSyntax() is TypeDeclarationSyntax typeSyntax
                && typeSyntax.Modifiers.Any(m => m.ValueText == "partial"))
                return true;

        return false;
    }

    private sealed class NamedTypeSymbolComparer : IEqualityComparer<INamedTypeSymbol>
    {
        public bool Equals(INamedTypeSymbol? x, INamedTypeSymbol? y)
        {
            return SymbolEqualityComparer.Default.Equals(x, y);
        }

        public int GetHashCode(INamedTypeSymbol obj)
        {
            return SymbolEqualityComparer.Default.GetHashCode(obj);
        }
    }
}

internal static class AttributeNames
{
    public const string CommandSpecAttribute = "Tenekon.CommandLine.Extensions.PolyType.Spec.CommandSpecAttribute";
}

internal static class GeneratorDiagnostics
{
    public static readonly DiagnosticDescriptor TypeMustBePartial = new(
        "TCL001",
        "CommandSpec type must be partial",
        "CommandSpec type '{0}' must be declared partial",
        "CommandSpecGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidType = new(
        "TCL002",
        "Invalid CommandSpec type",
        "CommandSpec type '{0}' must be a non-abstract, non-generic class",
        "CommandSpecGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}