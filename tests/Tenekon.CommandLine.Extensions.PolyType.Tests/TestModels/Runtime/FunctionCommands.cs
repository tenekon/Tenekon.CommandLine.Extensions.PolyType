using System.ComponentModel.DataAnnotations;
using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

[GenerateShapeFor(typeof(FunctionRootCommand))]
[GenerateShapeFor(typeof(FunctionChildCommand))]
[GenerateShapeFor(typeof(FunctionParentedRootCommand))]
[GenerateShapeFor(typeof(GenericFunctionCommand<int>))]
[GenerateShapeFor(typeof(FunctionParentCommand))]
public partial class FunctionWitness;

[CommandSpec]
public delegate void FunctionRootCommand(
    CommandRuntimeContext context,
    [OptionSpec(Name = "opt")]
    [Display(Description = "option-display-message")]
    string option,
    [ArgumentSpec(Name = "arg")]
    [Display(Description = "argument-display-message")]
    int argument,
    [DirectiveSpec(Name = "trace")] string directive,
    DiDependency dependency,
    CancellationToken token);

[CommandSpec]
public delegate void FunctionChildCommand();

[CommandSpec(Children = [typeof(FunctionChildCommand)])]
[GenerateShape]
public partial class FunctionParentCommand
{
    public void Run() { }
}

[CommandSpec(Parent = typeof(FunctionParentCommand))]
public delegate void FunctionParentedRootCommand();

[CommandSpec]
public delegate void GenericFunctionCommand<T>(T value);

internal static class FunctionCommandLog
{
    public static string? LastOption { get; set; }
    public static int LastArgument { get; set; }
    public static string? LastDirective { get; set; }
    public static string? LastServiceValue { get; set; }
    public static bool ContextSeen { get; set; }
    public static bool TokenCanceled { get; set; }

    public static void Reset()
    {
        LastOption = null;
        LastArgument = 0;
        LastDirective = null;
        LastServiceValue = null;
        ContextSeen = false;
        TokenCanceled = false;
    }
}
