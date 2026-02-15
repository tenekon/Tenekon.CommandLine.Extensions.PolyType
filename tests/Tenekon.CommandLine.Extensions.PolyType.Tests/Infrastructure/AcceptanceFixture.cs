using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Tenekon.CommandLine.Extensions.PolyType.SourceGenerator;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;

public sealed class AcceptanceFixture
{
    private static readonly CSharpParseOptions s_parseOptions = new(LanguageVersion.Preview);

    public IReadOnlyList<DiagnosticCaseResult> DiagnosticCases { get; } = BuildDiagnosticCases();

    private static IReadOnlyList<DiagnosticCaseResult> BuildDiagnosticCases()
    {
        var cases = new[]
        {
            new DiagnosticCase(
                "NotPartialCommand",
                Source: """
                using Tenekon.CommandLine.Extensions.PolyType.Spec;

                [CommandSpec]
                public class NotPartialCommand
                {
                }
                """,
                ["TCL001"]),
            new DiagnosticCase(
                "AbstractCommand",
                Source: """
                using Tenekon.CommandLine.Extensions.PolyType.Spec;

                [CommandSpec]
                public abstract partial class AbstractCommand
                {
                }
                """,
                ["TCL002"]),
            new DiagnosticCase(
                "ValidCommand",
                Source: """
                using Tenekon.CommandLine.Extensions.PolyType.Spec;

                [CommandSpec]
                public partial class ValidCommand
                {
                }
                """,
                [])
        };

        var results = new List<DiagnosticCaseResult>();
        foreach (var diagnosticCase in cases)
        {
            var compilation = CreateCompilation(diagnosticCase.Source);
            var analyzer = new CommandSpecDiagnosticsAnalyzer();
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);
            var diagnostics = compilation.WithAnalyzers(analyzers)
                .GetAnalyzerDiagnosticsAsync()
                .GetAwaiter()
                .GetResult()
                .Select(diagnostic => diagnostic.Id)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            results.Add(new DiagnosticCaseResult(diagnosticCase.ClassName, diagnosticCase.ExpectedIds, diagnostics));
        }

        return results;
    }

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, s_parseOptions);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CommandSpecAttribute).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "AcceptanceDiagnostics",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}