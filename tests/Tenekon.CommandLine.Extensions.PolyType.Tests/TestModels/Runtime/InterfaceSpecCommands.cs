using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

[GenerateShapeFor(typeof(IInterfaceSpecOption))]
[GenerateShapeFor(typeof(IInterfaceSpecArgument))]
[GenerateShapeFor(typeof(IInterfaceSpecConflict))]
[GenerateShapeFor(typeof(IInterfaceSpecConflict2))]
[GenerateShapeFor(typeof(IInterfaceSpecExplicit))]
[GenerateShapeFor(typeof(IInterfaceSpecDefault))]
[GenerateShapeFor(typeof(IInterfaceSpecAlias1))]
[GenerateShapeFor(typeof(IInterfaceSpecAlias2))]
[GenerateShapeFor(typeof(IInterfaceSpecInherited))]
[GenerateShapeFor(typeof(IInterfaceSpecInheritedBase))]
[GenerateShapeFor(typeof(IInterfaceSpecInheritedDerived))]
[GenerateShapeFor(typeof(IInterfaceSpecBaseOnly))]
[GenerateShapeFor(typeof(IInterfaceSpecChainBase))]
[GenerateShapeFor(typeof(IInterfaceSpecChainDerived))]
public partial class InterfaceSpecShapeWitness
{
}

public interface IInterfaceSpecOption
{
    [OptionSpec(Name = "iface-option")]
    string OptionValue { get; set; }
}

public interface IInterfaceSpecArgument
{
    [ArgumentSpec(Name = "iface-arg")]
    string ArgumentValue { get; set; }
}

public interface IInterfaceSpecConflict
{
    [OptionSpec(Name = "shared")]
    string Shared { get; set; }
}

public interface IInterfaceSpecConflict2
{
    [OptionSpec(Name = "shared")]
    string Shared { get; set; }
}

public interface IInterfaceSpecExplicit
{
    [OptionSpec(Name = "explicit")]
    string ExplicitValue { get; set; }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class InterfaceSpecCommand : IInterfaceSpecOption, IInterfaceSpecArgument
{
    public string OptionValue { get; set; } = "";
    public string ArgumentValue { get; set; } = "";

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class InterfaceSpecConflictCommand : IInterfaceSpecOption
{
    [OptionSpec(Name = "iface-option")]
    public string OptionValue { get; set; } = "";

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class InterfaceSpecMultipleConflictCommand : IInterfaceSpecConflict, IInterfaceSpecConflict2
{
    public string Shared { get; set; } = "";

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class InterfaceSpecExplicitCommand : IInterfaceSpecExplicit
{
    string IInterfaceSpecExplicit.ExplicitValue { get; set; } = "";

    public void Run() { }
}

public interface IInterfaceSpecDefault
{
    [OptionSpec(Name = "default-opt")]
    string? DefaultValue { get; set; }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class InterfaceSpecDefaultCommand : IInterfaceSpecDefault
{
    public string? DefaultValue { get; set; } = "from-default";

    public void Run() { }
}

public interface IInterfaceSpecAlias1
{
    [OptionSpec(Name = "same")]
    string Value1 { get; set; }
}

public interface IInterfaceSpecAlias2
{
    [OptionSpec(Name = "same")]
    string Value2 { get; set; }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class InterfaceSpecAliasCollisionCommand : IInterfaceSpecAlias1, IInterfaceSpecAlias2
{
    public string Value1 { get; set; } = "";
    public string Value2 { get; set; } = "";

    public void Run() { }
}

public interface IInterfaceSpecInherited
{
    [OptionSpec(Name = "base-opt")]
    string BaseValue { get; set; }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class InterfaceSpecBaseCommand : IInterfaceSpecInherited
{
    public string BaseValue { get; set; } = "";

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class InterfaceSpecDerivedCommand : InterfaceSpecBaseCommand
{
    public void RunDerived() { }
}

public interface IInterfaceSpecInheritedBase
{
    [OptionSpec(Name = "base-opt")]
    string Value { get; set; }
}

public interface IInterfaceSpecInheritedDerived : IInterfaceSpecInheritedBase
{
    [OptionSpec(Name = "derived-opt")]
    new string Value { get; set; }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class InterfaceSpecInheritedOverrideCommand : IInterfaceSpecInheritedDerived
{
    public string Value { get; set; } = "";

    public void Run() { }
}

public sealed class InterfaceSpecInheritedOverrideTarget : IInterfaceSpecInheritedDerived
{
    public string Value { get; set; } = "";
}

public interface IInterfaceSpecBaseOnly
{
    [OptionSpec(Name = "base-iface-opt")]
    string Value { get; set; }
}

public class InterfaceSpecBaseOnlyCommand : IInterfaceSpecBaseOnly
{
    public string Value { get; set; } = "";
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class InterfaceSpecDerivedFromBaseOnlyCommand : InterfaceSpecBaseOnlyCommand
{
    public void Run() { }
}

public interface IInterfaceSpecChainBase
{
    [OptionSpec(Name = "chain-base-opt")]
    string BaseValue { get; set; }
}

public interface IInterfaceSpecChainDerived : IInterfaceSpecChainBase
{
    [OptionSpec(Name = "chain-derived-opt")]
    string DerivedValue { get; set; }
}

public class InterfaceSpecChainBaseCommand : IInterfaceSpecChainDerived
{
    public string BaseValue { get; set; } = "";
    public string DerivedValue { get; set; } = "";
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class InterfaceSpecChainDerivedCommand : InterfaceSpecChainBaseCommand
{
    public void Run() { }
}

public sealed class InterfaceSpecTarget : IInterfaceSpecOption, IInterfaceSpecArgument
{
    public string OptionValue { get; set; } = "";
    public string ArgumentValue { get; set; } = "";
}

public sealed class InterfaceSpecOptionTarget : IInterfaceSpecOption
{
    public string OptionValue { get; set; } = "";
}

public sealed class InterfaceSpecArgumentTarget : IInterfaceSpecArgument
{
    public string ArgumentValue { get; set; } = "";
}