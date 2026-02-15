using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

[CommandSpec(Description = "Nested root")]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RootWithChildrenCommand
{
    [OptionSpec]
    public string? RootOption { get; set; } = "root";

    public void Run() { }

    [CommandSpec(Order = 2, Description = "Child B")]
    [GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
    public partial class ChildBCommand
    {
        [OptionSpec]
        public string? ChildBOption { get; set; } = "child-b";

        public RootWithChildrenCommand Root { get; set; } = null!;

        public void Run() { }
    }

    [CommandSpec(Order = 1, Description = "Child A")]
    [GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
    public partial class ChildACommand
    {
        [OptionSpec]
        public string? ChildAOption { get; set; } = "child-a";

        public RootWithChildrenCommand Root { get; set; } = null!;

        public void Run() { }
    }
}