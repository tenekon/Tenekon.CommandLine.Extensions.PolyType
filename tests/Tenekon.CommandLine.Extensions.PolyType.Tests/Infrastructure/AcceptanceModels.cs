namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;

internal sealed record DiagnosticCase(string ClassName, string Source, IReadOnlyList<string> ExpectedIds);

public sealed record DiagnosticCaseResult(
    string ClassName,
    IReadOnlyList<string> ExpectedIds,
    IReadOnlyList<string> ActualIds);
