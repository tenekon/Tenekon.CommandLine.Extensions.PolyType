using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class MethodRootCommand
{
    [CommandSpec(Children = [typeof(MethodChildCommand)])]
    public void RunChild([OptionSpec] int count) { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class MethodChildCommand
{
    [OptionSpec]
    public string Name { get; set; } = string.Empty;

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.AllPublic)]
public partial class StaticMethodCommand
{
    [CommandSpec]
    public static void DoWork() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class InvalidMethodParentCommand
{
    [CommandSpec(Parent = typeof(MethodChildCommand))]
    public void RunInvalid() { }
}

internal static class MethodCommandLog
{
    public static string? LastOption { get; set; }
    public static int LastArgument { get; set; }
    public static string? LastDirective { get; set; }
    public static string? LastServiceValue { get; set; }
    public static bool ContextSeen { get; set; }
    public static bool TokenCanceled { get; set; }
    public static object? LastInstance { get; set; }
    public static string? LastInstanceName { get; set; }

    public static void Reset()
    {
        LastOption = null;
        LastArgument = 0;
        LastDirective = null;
        LastServiceValue = null;
        ContextSeen = false;
        TokenCanceled = false;
        LastInstance = null;
        LastInstanceName = null;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class OverloadMissingNameCommand
{
    [CommandSpec]
    public void Execute() { }

    [CommandSpec]
    public void Execute(int value) { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class OverloadNamedOkCommand
{
    [CommandSpec(Name = "first")]
    public void Execute() { }

    [CommandSpec(Name = "second")]
    public void Execute(int value) { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class OverloadNamedCollisionCommand
{
    [CommandSpec(Name = "dup")]
    public void Execute() { }

    [CommandSpec(Name = "dup")]
    public void Execute(int value) { }
}

public interface IInterfaceMethodCommand
{
    [CommandSpec]
    void Execute();
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.AllPublic)]
public partial class InterfaceMethodCommand : IInterfaceMethodCommand
{
    public void Execute() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.AllPublic)]
public partial class GenericMethodCommand
{
    [CommandSpec]
    public void RunGeneric<T>() { }
}

public class GenericBaseCommand<T>
{
    [CommandSpec]
    public void Execute() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.AllPublic)]
public partial class GenericBaseDerivedCommand : GenericBaseCommand<int>
{
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class MethodInvocationCommand
{
    [CommandSpec(Name = "invoke")]
    public void Invoke(
        CommandRuntimeContext context,
        [OptionSpec(Name = "opt")] string option,
        [ArgumentSpec(Name = "arg")] int argument,
        [DirectiveSpec(Name = "trace")] string directive,
        DiDependency dependency,
        CancellationToken token)
    {
        MethodCommandLog.LastOption = option;
        MethodCommandLog.LastArgument = argument;
        MethodCommandLog.LastDirective = directive;
        MethodCommandLog.LastServiceValue = dependency.Value;
        MethodCommandLog.ContextSeen = context is not null;
        MethodCommandLog.TokenCanceled = token.IsCancellationRequested;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class MethodInstanceCommand
{
    [OptionSpec(Name = "name")]
    public string Name { get; set; } = "default";

    [CommandSpec(Name = "child")]
    public void Child()
    {
        MethodCommandLog.LastInstance = this;
        MethodCommandLog.LastInstanceName = Name;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class MethodContextNotFirstCommand
{
    [CommandSpec(Name = "invoke")]
    public void Invoke(int value, CommandRuntimeContext context) { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class MethodTokenNotLastCommand
{
    [CommandSpec(Name = "invoke")]
    public void Invoke(CancellationToken token, int value) { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class MethodSpecOnContextCommand
{
    [CommandSpec(Name = "invoke")]
    public void Invoke([OptionSpec] CommandRuntimeContext context) { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class MethodSpecOnTokenCommand
{
    [CommandSpec(Name = "invoke")]
    public void Invoke([OptionSpec] CancellationToken token) { }
}