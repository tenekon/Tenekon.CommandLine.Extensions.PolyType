using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime;

[Collection("HandlerLog")]
public class BindingMatrixTests
{
    private const string OptionValue = "opt-value";
    private const int ArgumentValue = 7;
    private const string DirectiveValue = "dir";
    private const string ServiceValue = "service";
    private const string FunctionValue = "registry";
    private const string ServiceFunctionValue = "service-function";

    [Theory]
    [CombinatorialData]
    public async Task Invoke_TypeRun_BindsParameters(
        [CombinatorialValues(BindingMatrixStage.Constructor, BindingMatrixStage.Run)] BindingMatrixStage stage,
        [CombinatorialValues(
            BindingMatrixBindingKind.Parent,
            BindingMatrixBindingKind.Context,
            BindingMatrixBindingKind.Token,
            BindingMatrixBindingKind.Service,
            BindingMatrixBindingKind.Function)]
        BindingMatrixBindingKind kind)
    {
        var state = await InvokeAsync(InvocationKind.TypeRun);
        var entry = BindingMatrixLog.Get(ToSite(stage));

        AssertEntry(entry, state.RootInstance, state.FakeParent, kind);
    }

    [Theory]
    [CombinatorialData]
    public async Task Invoke_TypeRunAsync_BindsParameters(
        [CombinatorialValues(BindingMatrixStage.Constructor, BindingMatrixStage.RunAsync)] BindingMatrixStage stage,
        [CombinatorialValues(
            BindingMatrixBindingKind.Parent,
            BindingMatrixBindingKind.Context,
            BindingMatrixBindingKind.Token,
            BindingMatrixBindingKind.Service,
            BindingMatrixBindingKind.Function)]
        BindingMatrixBindingKind kind)
    {
        var state = await InvokeAsync(InvocationKind.TypeRunAsync);
        var entry = BindingMatrixLog.Get(ToSite(stage));

        AssertEntry(entry, state.RootInstance, state.FakeParent, kind);
    }

    [Theory]
    [CombinatorialData]
    public async Task Invoke_Method_BindsParameters(
        [CombinatorialValues(
            BindingMatrixBindingKind.Context,
            BindingMatrixBindingKind.Token,
            BindingMatrixBindingKind.Service,
            BindingMatrixBindingKind.Function,
            BindingMatrixBindingKind.Option,
            BindingMatrixBindingKind.Argument,
            BindingMatrixBindingKind.Directive)]
        BindingMatrixBindingKind kind)
    {
        var state = await InvokeAsync(InvocationKind.Method);
        var entry = BindingMatrixLog.Get(BindingMatrixSite.Method);

        AssertEntry(entry, state.RootInstance, state.FakeParent, kind);
    }

    [Theory]
    [CombinatorialData]
    public async Task Invoke_Function_BindsParameters(
        [CombinatorialValues(
            BindingMatrixBindingKind.Parent,
            BindingMatrixBindingKind.Context,
            BindingMatrixBindingKind.Token,
            BindingMatrixBindingKind.Service,
            BindingMatrixBindingKind.Function,
            BindingMatrixBindingKind.Option,
            BindingMatrixBindingKind.Argument,
            BindingMatrixBindingKind.Directive)]
        BindingMatrixBindingKind kind)
    {
        var state = await InvokeAsync(InvocationKind.Function);
        var entry = BindingMatrixLog.Get(BindingMatrixSite.Function);

        AssertEntry(entry, state.RootInstance, state.FakeParent, kind);
    }

    private static async Task<BindingMatrixState> InvokeAsync(InvocationKind kind)
    {
        BindingMatrixLog.Reset();
        var settings = new CommandRuntimeSettings
        {
            ShowHelpOnEmptyCommand = false,
            AllowFunctionResolutionFromServices = true
        };

        var services = new ServiceCollection();
        var dependency = new BindingMatrixDependency(ServiceValue);
        var serviceCallback = new BindingMatrixCallback(() => ServiceFunctionValue);
        var fakeParent = new BindingMatrixRootCommand();
        services.AddSingleton(dependency);
        services.AddSingleton(serviceCallback);
        services.AddSingleton(fakeParent);
        var provider = services.BuildServiceProvider();
        var resolver = new ServiceProviderResolver(provider);
        var app = CommandRuntime.Factory.Object.Create<BindingMatrixRootCommand>(settings, resolver);
        app.FunctionRegistry.Set<BindingMatrixCallback>(() => FunctionValue);
        app.FunctionRegistry.Set<BindingMatrixFunctionCommand>((
            context,
            parent,
            option,
            argument,
            directive,
            callback,
            dep,
            token) =>
        {
            BindingMatrixLog.Record(
                new BindingMatrixEntry(
                    BindingMatrixSite.Function,
                    parent,
                    context is not null,
                    token.IsCancellationRequested,
                    token.CanBeCanceled,
                    callback(),
                    dep.Value,
                    option,
                    argument,
                    directive));
        });

        var args = kind switch
        {
            InvocationKind.TypeRun => new[] { "child-run" },
            InvocationKind.TypeRunAsync => new[] { "child-run-async" },
            InvocationKind.Method => new[]
                { "[trace-method:dir]", "method", "--opt-method", OptionValue, ArgumentValue.ToString() },
            InvocationKind.Function => new[]
                { "[trace-function:dir]", "function-child", "--opt-function", OptionValue, ArgumentValue.ToString() },
            _ => Array.Empty<string>()
        };

        var result = app.Parse(args);
        result.ParseResult.Errors.Count.ShouldBe(expected: 0);
        using var cts = new CancellationTokenSource();

#pragma warning disable xUnit1051
        await result.RunAsync(cts.Token);
#pragma warning restore xUnit1051

        var rootInstance = result.Bind<BindingMatrixRootCommand>();
        return new BindingMatrixState(rootInstance, fakeParent);
    }

    private static void AssertEntry(
        BindingMatrixEntry entry,
        BindingMatrixRootCommand rootInstance,
        BindingMatrixRootCommand fakeParent,
        BindingMatrixBindingKind kind)
    {
        switch (kind)
        {
            case BindingMatrixBindingKind.Parent:
                entry.Parent.ShouldBe(rootInstance);
                entry.Parent.ShouldNotBe(fakeParent);
                break;
            case BindingMatrixBindingKind.Context:
                entry.ContextSeen.ShouldBeTrue();
                break;
            case BindingMatrixBindingKind.Token:
                entry.TokenCanBeCanceled.ShouldBeTrue();
                break;
            case BindingMatrixBindingKind.Service:
                entry.ServiceValue.ShouldBe(ServiceValue);
                break;
            case BindingMatrixBindingKind.Function:
                entry.CallbackValue.ShouldBe(FunctionValue);
                break;
            case BindingMatrixBindingKind.Option:
                entry.OptionValue.ShouldBe(OptionValue);
                break;
            case BindingMatrixBindingKind.Argument:
                entry.ArgumentValue.ShouldBe(ArgumentValue);
                break;
            case BindingMatrixBindingKind.Directive:
                entry.DirectiveValue.ShouldBe(DirectiveValue);
                break;
        }
    }

    private static BindingMatrixSite ToSite(BindingMatrixStage stage)
    {
        return stage switch
        {
            BindingMatrixStage.Constructor => BindingMatrixSite.Constructor,
            BindingMatrixStage.Run => BindingMatrixSite.Run,
            BindingMatrixStage.RunAsync => BindingMatrixSite.RunAsync,
            BindingMatrixStage.Method => BindingMatrixSite.Method,
            BindingMatrixStage.Function => BindingMatrixSite.Function,
            _ => BindingMatrixSite.Run
        };
    }

    public enum BindingMatrixStage
    {
        Constructor,
        Run,
        RunAsync,
        Method,
        Function
    }

    private enum InvocationKind
    {
        TypeRun,
        TypeRunAsync,
        Method,
        Function
    }

    public enum BindingMatrixBindingKind
    {
        Parent,
        Context,
        Token,
        Service,
        Function,
        Option,
        Argument,
        Directive
    }

    private readonly record struct BindingMatrixState(
        BindingMatrixRootCommand RootInstance,
        BindingMatrixRootCommand FakeParent);
}