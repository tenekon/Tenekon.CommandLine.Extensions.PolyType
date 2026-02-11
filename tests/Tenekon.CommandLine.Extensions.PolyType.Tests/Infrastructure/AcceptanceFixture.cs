using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Tenekon.CommandLine.Extensions.PolyType.SourceGeneration;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;

public sealed class AcceptanceFixture
{
    private static readonly CSharpParseOptions s_parseOptions = new(LanguageVersion.Preview);

    public AcceptanceFixture()
    {
        DiagnosticCases = BuildDiagnosticCases();
    }

    public IReadOnlyList<DiagnosticCaseResult> DiagnosticCases { get; }

    private static IReadOnlyList<DiagnosticCaseResult> BuildDiagnosticCases()
    {
        var cases = new[]
        {
            new DiagnosticCase(
                "NotPartialCommand",
                """
                using Tenekon.CommandLine.Extensions.PolyType;

                [CommandSpec]
                public class NotPartialCommand
                {
                }
                """,
                ["TCL001"]),
            new DiagnosticCase(
                "AbstractCommand",
                """
                using Tenekon.CommandLine.Extensions.PolyType;

                [CommandSpec]
                public abstract partial class AbstractCommand
                {
                }
                """,
                ["TCL002"]),
            new DiagnosticCase(
                "ValidCommand",
                """
                using Tenekon.CommandLine.Extensions.PolyType;

                [CommandSpec]
                public partial class ValidCommand
                {
                }
                """,
                Array.Empty<string>())
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

            results.Add(new DiagnosticCaseResult(
                diagnosticCase.ClassName,
                diagnosticCase.ExpectedIds,
                diagnostics));
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
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
