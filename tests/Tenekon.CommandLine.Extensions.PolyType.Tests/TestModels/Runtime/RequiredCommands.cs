using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RequiredOptionCommand
{
    [OptionSpec]
    public string RequiredOption { get; set; } = null!;

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class OptionalOptionCommand
{
    [OptionSpec(Name = "option")]
    public string Option { get; set; } = "default";

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class ValueTypeOptionCommand
{
    [OptionSpec]
    public int Count { get; set; }

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class NullableOptionCommand
{
    [OptionSpec(Name = "option")]
    public string? Option { get; set; }

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RequiredArgumentCommand
{
    [ArgumentSpec]
    public string RequiredArg { get; set; } = null!;

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class EnumerableArgumentCommand
{
    [ArgumentSpec]
    public string[] Items { get; set; } = null!;

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class OptionalArgumentCommand
{
    [ArgumentSpec]
    public string? OptionalArg { get; set; }

    public void Run() { }
}