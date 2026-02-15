using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

[CommandSpec(
    Children =
    [
        typeof(ParentInjectionChildRunCommand),
        typeof(ParentInjectionChildRunAsyncCommand),
        typeof(ParentInjectionFunctionCommand)
    ])]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class ParentInjectionRootCommand
{
    public void Run() { }
}

[CommandSpec(Name = "child-run", Parent = typeof(ParentInjectionRootCommand))]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class ParentInjectionChildRunCommand
{
    public ParentInjectionRootCommand ParentFromConstructor { get; }

    public ParentInjectionChildRunCommand(
        ParentInjectionRootCommand parent,
        CommandRuntimeContext context,
        CancellationToken cancellationToken)
    {
        ParentFromConstructor = parent;
        ParentInjectionLog.ConstructorParent = parent;
        ParentInjectionLog.ConstructorContextSeen = context is not null;
        ParentInjectionLog.ConstructorTokenCanceled = cancellationToken.IsCancellationRequested;
    }

    public void Run(ParentInjectionRootCommand parent)
    {
        ParentInjectionLog.RunParent = parent;
    }
}

[CommandSpec(Name = "child-run-async", Parent = typeof(ParentInjectionRootCommand))]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class ParentInjectionChildRunAsyncCommand
{
    public ParentInjectionChildRunAsyncCommand(ParentInjectionRootCommand parent, CancellationToken cancellationToken)
    {
        ParentInjectionLog.RunAsyncConstructorParent = parent;
        ParentInjectionLog.RunAsyncConstructorTokenCanceled = cancellationToken.IsCancellationRequested;
    }

    public Task<int> RunAsync(ParentInjectionRootCommand parent, CancellationToken cancellationToken)
    {
        ParentInjectionLog.RunAsyncParent = parent;
        ParentInjectionLog.RunAsyncTokenCanceled = cancellationToken.IsCancellationRequested;
        return Task.FromResult(result: 0);
    }
}

[CommandSpec(Name = "function-child", Parent = typeof(ParentInjectionRootCommand))]
public delegate void ParentInjectionFunctionCommand(ParentInjectionRootCommand parent);

internal static class ParentInjectionLog
{
    public static ParentInjectionRootCommand? ConstructorParent { get; set; }
    public static bool ConstructorContextSeen { get; set; }
    public static bool ConstructorTokenCanceled { get; set; }
    public static ParentInjectionRootCommand? RunParent { get; set; }
    public static ParentInjectionRootCommand? RunAsyncConstructorParent { get; set; }
    public static bool RunAsyncConstructorTokenCanceled { get; set; }
    public static ParentInjectionRootCommand? RunAsyncParent { get; set; }
    public static bool RunAsyncTokenCanceled { get; set; }
    public static ParentInjectionRootCommand? FunctionParent { get; set; }

    public static void Reset()
    {
        ConstructorParent = null;
        ConstructorContextSeen = false;
        ConstructorTokenCanceled = false;
        RunParent = null;
        RunAsyncConstructorParent = null;
        RunAsyncConstructorTokenCanceled = false;
        RunAsyncParent = null;
        RunAsyncTokenCanceled = false;
        FunctionParent = null;
    }
}

[GenerateShapeFor(typeof(ParentInjectionFunctionCommand))]
public partial class ParentInjectionWitness;