using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

[CommandSpec(Description = "Basic root command")]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class BasicRootCommand
{
    [OptionSpec(Description = "Option 1")]
    public string Option1 { get; set; } = "default-opt";

    [OptionSpec(Alias = "-a", Aliases = ["--alias2"])]
    public string AliasOption { get; set; } = "alias-default";

    [OptionSpec(AllowMultipleArgumentsPerToken = true)]
    public string[] Tags { get; set; } = [];

    [OptionSpec]
    public string RequiredOption { get; set; } = null!;

    [OptionSpec]
    public int Count { get; set; }

    [ArgumentSpec(Description = "Argument 1")]
    public string Argument1 { get; set; } = "arg-default";

    [ArgumentSpec]
    public string? OptionalArgument { get; set; }

    public int Run()
    {
        return 0;
    }
}
