using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

internal static class EdgeCaseLog
{
    public static int RunUnsupportedParamCount { get; set; }
    public static int RunAsyncCancellationCount { get; set; }
    public static int RunPrivateCount { get; set; }
    public static int RunOverloadCount { get; set; }
    public static int RunOverloadContextCount { get; set; }
    public static int RunContextNotFirstCount { get; set; }
    public static int RunTokenNotLastCount { get; set; }

    public static void Reset()
    {
        RunUnsupportedParamCount = 0;
        RunAsyncCancellationCount = 0;
        RunPrivateCount = 0;
        RunOverloadCount = 0;
        RunOverloadContextCount = 0;
        RunContextNotFirstCount = 0;
        RunTokenNotLastCount = 0;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunWithContextNotFirstCommand
{
    public void Run(DiDependency dependency, CommandRuntimeContext context)
    {
        EdgeCaseLog.RunContextNotFirstCount++;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunAsyncWithCancellationTokenCommand
{
    public Task RunAsync(CancellationToken token)
    {
        EdgeCaseLog.RunAsyncCancellationCount++;
        return Task.CompletedTask;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunAsyncWithTokenNotLastCommand
{
    public Task RunAsync(CancellationToken token, DiDependency dependency)
    {
        EdgeCaseLog.RunTokenNotLastCount++;
        return Task.CompletedTask;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunPrivateCommand
{
    private void Run()
    {
        EdgeCaseLog.RunPrivateCount++;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RunOverloadConflictCommand
{
    public void Run()
    {
        EdgeCaseLog.RunOverloadCount++;
    }

    public void Run(CommandRuntimeContext context)
    {
        EdgeCaseLog.RunOverloadContextCount++;
    }
}

public sealed class RequiredService(string value)
{
    public string Value { get; } = value;
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class RequiredCtorParamCommand(RequiredService service)
{
    public RequiredService Service { get; } = service;

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class OptionalCtorParamCommand(RequiredService? service = null)
{
    public RequiredService? Service { get; } = service;

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class CollisionRootCommand
{
    [OptionSpec(Name = "shared", Alias = "-x")]
    public bool RootFlag { get; set; }

    public void Run() { }

    [CommandSpec]
    [GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
    public partial class CollisionChildCommand
    {
        [OptionSpec(Name = "shared", Alias = "-x")]
        public bool ChildFlag { get; set; }

        public void Run() { }
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class AliasCollisionRootCommand
{
    [OptionSpec(Name = "one", Alias = "-x")]
    public bool One { get; set; }

    public void Run() { }

    [CommandSpec]
    [GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
    public partial class AliasCollisionChildCommand
    {
        [OptionSpec(Name = "two", Alias = "-x")]
        public bool Two { get; set; }

        public void Run() { }
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class ExplicitRequiredOptionCommand
{
    [OptionSpec(Name = "required", Required = true)]
    public string? Option { get; set; }

    public void Run() { }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class ExplicitRequiredValueTypeOptionCommand
{
    [OptionSpec(Name = "count", Required = true)]
    public int Count { get; set; }

    public void Run() { }
}