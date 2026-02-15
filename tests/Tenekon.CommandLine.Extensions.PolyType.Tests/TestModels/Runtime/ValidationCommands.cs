using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class ValidationCommand
{
    [OptionSpec(
        Name = "option",
        AllowedValues = ["A", "B"],
        ValidationPattern = "^[a-z]+$",
        ValidationMessage = "pattern-error")]
    public string? Option { get; set; }

    [ArgumentSpec(Name = "argument", AllowedValues = ["1", "2"])]
    public string Argument { get; set; } = "1";

    public void Run() { }
}