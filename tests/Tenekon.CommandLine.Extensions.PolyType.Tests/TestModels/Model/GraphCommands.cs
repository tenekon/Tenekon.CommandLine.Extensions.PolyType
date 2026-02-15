using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class ConflictingParentRoot
{
    public void Run() { }

    [CommandSpec(Parent = typeof(OtherRoot))]
    [GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
    public partial class ConflictingChild
    {
        public void Run() { }
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class OtherRoot
{
    public void Run() { }
}

[CommandSpec(Children = [typeof(CycleB)])]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class CycleA
{
    public void Run() { }
}

[CommandSpec(Children = [typeof(CycleA)])]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class CycleB
{
    public void Run() { }
}

[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class MissingCommandSpec
{
    public void Run() { }
}