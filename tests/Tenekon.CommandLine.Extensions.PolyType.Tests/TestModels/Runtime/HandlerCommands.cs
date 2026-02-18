using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

internal static class HandlerLog
{
    public static int RunCount { get; set; }
    public static int RunAsyncCount { get; set; }
    public static int ContextCount { get; set; }
    public static CommandRuntimeContext? LastContext { get; set; }
    public static string? LastServiceValue { get; set; }
    public static bool LastTokenCanceled { get; set; }
    public static bool OptionalServiceWasNull { get; set; }

    public static void Reset()
    {
        RunCount = 0;
        RunAsyncCount = 0;
        ContextCount = 0;
        LastContext = null;
        LastServiceValue = null;
        LastTokenCanceled = false;
        OptionalServiceWasNull = false;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunCommand
{
    [OptionSpec(Name = "trigger")]
    public bool Trigger { get; set; }

    public void Run()
    {
        HandlerLog.RunCount++;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunReturnsIntCommand
{
    [OptionSpec(Name = "trigger")]
    public bool Trigger { get; set; }

    public int Run()
    {
        HandlerLog.RunCount++;
        return 7;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunWithContextCommand
{
    [OptionSpec(Name = "trigger")]
    public bool Trigger { get; set; }

    public void Run(CommandRuntimeContext context)
    {
        HandlerLog.RunCount++;
        HandlerLog.LastContext = context;
        HandlerLog.ContextCount++;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunWithServiceCommand
{
    [OptionSpec(Name = "trigger")]
    public bool Trigger { get; set; }

    public void Run(DiDependency dependency)
    {
        HandlerLog.RunCount++;
        HandlerLog.LastServiceValue = dependency.Value;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunWithContextAndServiceCommand
{
    [OptionSpec(Name = "trigger")]
    public bool Trigger { get; set; }

    public void Run(CommandRuntimeContext context, DiDependency dependency)
    {
        HandlerLog.RunCount++;
        HandlerLog.ContextCount++;
        HandlerLog.LastContext = context;
        HandlerLog.LastServiceValue = dependency.Value;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunAsyncCommand
{
    [OptionSpec(Name = "trigger")]
    public bool Trigger { get; set; }

    public Task RunAsync()
    {
        HandlerLog.RunAsyncCount++;
        return Task.CompletedTask;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunAsyncWithServiceAndTokenCommand
{
    [OptionSpec(Name = "trigger")]
    public bool Trigger { get; set; }

    public Task RunAsync(DiDependency dependency, CancellationToken token)
    {
        HandlerLog.RunAsyncCount++;
        HandlerLog.LastServiceValue = dependency.Value;
        HandlerLog.LastTokenCanceled = token.IsCancellationRequested;
        return Task.CompletedTask;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunAsyncReturnsIntCommand
{
    [OptionSpec(Name = "trigger")]
    public bool Trigger { get; set; }

    public Task<int> RunAsync()
    {
        HandlerLog.RunAsyncCount++;
        return Task.FromResult(result: 5);
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunAsyncWithContextCommand
{
    [OptionSpec(Name = "trigger")]
    public bool Trigger { get; set; }

    public Task<int> RunAsync(CommandRuntimeContext context)
    {
        HandlerLog.RunAsyncCount++;
        HandlerLog.LastContext = context;
        HandlerLog.ContextCount++;
        return Task.FromResult(result: 9);
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunAndRunAsyncCommand
{
    [OptionSpec(Name = "trigger")]
    public bool Trigger { get; set; }

    public void Run()
    {
        HandlerLog.RunCount++;
    }

    public Task RunAsync()
    {
        HandlerLog.RunAsyncCount++;
        return Task.CompletedTask;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunWithRequiredServiceCommand
{
    [OptionSpec(Name = "trigger")]
    public bool Trigger { get; set; }

    public void Run(RequiredService service)
    {
        HandlerLog.RunCount++;
        HandlerLog.LastServiceValue = service.Value;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunWithOptionalServiceCommand
{
    [OptionSpec(Name = "trigger")]
    public bool Trigger { get; set; }

    public void Run(RequiredService? service = null)
    {
        HandlerLog.RunCount++;
        HandlerLog.OptionalServiceWasNull = service is null;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunWithOptionalValueTypeServiceCommand
{
    [OptionSpec(Name = "trigger")]
    public bool Trigger { get; set; }

    public void Run(int level = 7)
    {
        HandlerLog.RunCount++;
        HandlerLog.LastServiceValue = level.ToString();
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class NoRunCommand
{
    [OptionSpec(Name = "option")]
    public string Option { get; set; } = "value";
}
