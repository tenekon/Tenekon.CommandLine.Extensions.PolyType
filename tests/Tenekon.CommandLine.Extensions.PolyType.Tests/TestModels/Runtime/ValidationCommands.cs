using System.ComponentModel.DataAnnotations;
using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.AllPublic)]
public partial class ValidationCommand
{
    [OptionSpec(
        Name = "option",
        AllowedValues = ["A", "B"],
        ValidationPattern = "^[a-z]+$",
        ValidationMessage = "pattern-error")]
    [Display(Description = "option-display-message")]
    public string? Option { get; set; }

    [ArgumentSpec(Name = "argument", AllowedValues = ["1", "2"])]
    [Display(Description = "argument-display-message")]
    public string Argument { get; set; } = "1";

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class FileSystemValidationCommand
{
    [OptionSpec(Name = "file", ValidationRules = ValidationRules.ExistingFile)]
    public string File { get; set; } = "";

    [ArgumentSpec(Name = "dir", ValidationRules = ValidationRules.ExistingDirectory)]
    public string Directory { get; set; } = "";

    public void Run() { }
}
