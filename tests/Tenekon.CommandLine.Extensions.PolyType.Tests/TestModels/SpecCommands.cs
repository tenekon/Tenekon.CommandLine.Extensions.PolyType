using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class OptionSpecCommand
{
    [OptionSpec(
        Name = "--custom",
        Description = "desc",
        Hidden = true,
        HelpName = "VAL",
        Required = true,
        Arity = ArgumentArity.OneOrMore,
        AllowMultipleArgumentsPerToken = true,
        Recursive = true,
        Alias = "-c",
        Aliases = ["--c2"])]
    public string[] Values { get; set; } = [];

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class OptionDefaultCommand
{
    [OptionSpec]
    public string Name { get; set; } = "default";

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class ArgumentSpecCommand
{
    [ArgumentSpec(Description = "desc", HelpName = "ARG", Arity = ArgumentArity.ExactlyOne)]
    public string Value { get; set; } = "default";

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class ArgumentEnumerableCommand
{
    [ArgumentSpec]
    public string[] Items { get; set; } = null!;

    public void Run() { }
}
