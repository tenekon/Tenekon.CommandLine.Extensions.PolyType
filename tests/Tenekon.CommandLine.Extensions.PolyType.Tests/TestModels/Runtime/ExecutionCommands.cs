using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class BundlingCommand
{
    [OptionSpec(Alias = "-a")]
    public bool A { get; set; }

    [OptionSpec(Alias = "-b")]
    public bool B { get; set; }

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class ResponseFileCommand
{
    [OptionSpec]
    public string Value { get; set; } = "";

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class DiCommand(DiDependency dependency)
{
    [OptionSpec(Name = "trigger")]
    public bool Trigger { get; set; }

    public void Run()
    {
        DiLog.Value = dependency.Value;
    }
}

public sealed class DiDependency(string value)
{
    public string Value { get; } = value;
}

internal static class DiLog
{
    public static string? Value { get; set; }

    public static void Reset()
    {
        Value = null;
    }
}

[CommandSpec(TreatUnmatchedTokensAsErrors = true)]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class UnmatchedTokensErrorCommand
{
    public void Run() { }
}

[CommandSpec(TreatUnmatchedTokensAsErrors = false)]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class UnmatchedTokensAllowedCommand
{
    public void Run() { }
}