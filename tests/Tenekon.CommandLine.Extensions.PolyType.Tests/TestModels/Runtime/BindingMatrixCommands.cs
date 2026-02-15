using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

[CommandSpec(
    Children =
    [
        typeof(BindingMatrixChildRunCommand),
        typeof(BindingMatrixChildRunAsyncCommand),
        typeof(BindingMatrixFunctionCommand)
    ])]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class BindingMatrixRootCommand
{
    public void Run() { }

    [CommandSpec(Name = "method")]
    public void Method(
        CommandRuntimeContext context,
        [OptionSpec(Name = "opt-method")] string option,
        [ArgumentSpec(Name = "arg-method")] int argument,
        [DirectiveSpec(Name = "trace-method")] string directive,
        BindingMatrixCallback callback,
        BindingMatrixDependency dependency,
        CancellationToken token)
    {
        BindingMatrixLog.Record(
            new BindingMatrixEntry(
                BindingMatrixSite.Method,
                Parent: null,
                context is not null,
                token.IsCancellationRequested,
                token.CanBeCanceled,
                callback(),
                dependency.Value,
                option,
                argument,
                directive));
    }
}

[CommandSpec(Name = "child-run", Parent = typeof(BindingMatrixRootCommand))]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class BindingMatrixChildRunCommand
{
    public BindingMatrixChildRunCommand(
        BindingMatrixRootCommand parent,
        CommandRuntimeContext context,
        BindingMatrixCallback callback,
        BindingMatrixDependency dependency,
        CancellationToken token)
    {
        BindingMatrixLog.Record(
            new BindingMatrixEntry(
                BindingMatrixSite.Constructor,
                parent,
                context is not null,
                token.IsCancellationRequested,
                token.CanBeCanceled,
                callback(),
                dependency.Value,
                OptionValue: null,
                ArgumentValue: null,
                DirectiveValue: null));
    }

    public void Run(
        CommandRuntimeContext context,
        BindingMatrixRootCommand parent,
        BindingMatrixCallback callback,
        BindingMatrixDependency dependency,
        CancellationToken token)
    {
        BindingMatrixLog.Record(
            new BindingMatrixEntry(
                BindingMatrixSite.Run,
                parent,
                context is not null,
                token.IsCancellationRequested,
                token.CanBeCanceled,
                callback(),
                dependency.Value,
                OptionValue: null,
                ArgumentValue: null,
                DirectiveValue: null));
    }
}

[CommandSpec(Name = "child-run-async", Parent = typeof(BindingMatrixRootCommand))]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class BindingMatrixChildRunAsyncCommand
{
    public BindingMatrixChildRunAsyncCommand(
        BindingMatrixRootCommand parent,
        CommandRuntimeContext context,
        BindingMatrixCallback callback,
        BindingMatrixDependency dependency,
        CancellationToken token)
    {
        BindingMatrixLog.Record(
            new BindingMatrixEntry(
                BindingMatrixSite.Constructor,
                parent,
                context is not null,
                token.IsCancellationRequested,
                token.CanBeCanceled,
                callback(),
                dependency.Value,
                OptionValue: null,
                ArgumentValue: null,
                DirectiveValue: null));
    }

    public Task<int> RunAsync(
        CommandRuntimeContext context,
        BindingMatrixRootCommand parent,
        BindingMatrixCallback callback,
        BindingMatrixDependency dependency,
        CancellationToken token)
    {
        BindingMatrixLog.Record(
            new BindingMatrixEntry(
                BindingMatrixSite.RunAsync,
                parent,
                context is not null,
                token.IsCancellationRequested,
                token.CanBeCanceled,
                callback(),
                dependency.Value,
                OptionValue: null,
                ArgumentValue: null,
                DirectiveValue: null));
        return Task.FromResult(result: 0);
    }
}

[CommandSpec(Name = "function-child", Parent = typeof(BindingMatrixRootCommand))]
public delegate void BindingMatrixFunctionCommand(
    CommandRuntimeContext context,
    BindingMatrixRootCommand parent,
    [OptionSpec(Name = "opt-function")] string option,
    [ArgumentSpec(Name = "arg-function")] int argument,
    [DirectiveSpec(Name = "trace-function")] string directive,
    BindingMatrixCallback callback,
    BindingMatrixDependency dependency,
    CancellationToken token);

public delegate string BindingMatrixCallback();

public sealed class BindingMatrixDependency(string value)
{
    public string Value { get; } = value;
}

internal enum BindingMatrixSite
{
    Constructor,
    Run,
    RunAsync,
    Method,
    Function
}

internal sealed record BindingMatrixEntry(
    BindingMatrixSite Site,
    BindingMatrixRootCommand? Parent,
    bool ContextSeen,
    bool TokenCanceled,
    bool TokenCanBeCanceled,
    string? CallbackValue,
    string? ServiceValue,
    string? OptionValue,
    int? ArgumentValue,
    string? DirectiveValue);

internal static class BindingMatrixLog
{
    private static readonly Dictionary<BindingMatrixSite, BindingMatrixEntry> Entries = new();

    public static void Record(BindingMatrixEntry entry)
    {
        Entries[entry.Site] = entry;
    }

    public static BindingMatrixEntry Get(BindingMatrixSite site)
    {
        return Entries[site];
    }

    public static void Reset()
    {
        Entries.Clear();
    }
}

[GenerateShapeFor(typeof(BindingMatrixFunctionCommand))]
public partial class BindingMatrixFunctionWitness;