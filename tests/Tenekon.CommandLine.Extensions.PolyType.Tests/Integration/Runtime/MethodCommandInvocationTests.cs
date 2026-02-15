using Microsoft.Extensions.DependencyInjection;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime;

[Collection("HandlerLog")]
public class MethodCommandInvocationTests
{
    [Fact]
    public void Invoke_MethodCommand_BindsValuesAndServices()
    {
        MethodCommandLog.Reset();
        var settings = new CommandRuntimeSettings { ShowHelpOnEmptyCommand = false };
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("service"));
        var provider = services.BuildServiceProvider();
        var resolver = new ServiceProviderResolver(provider);
        var app = CommandRuntime.Factory.Object.Create<MethodInvocationCommand>(settings, resolver);

        var result = app.Parse(["[trace:dir]", "invoke", "--opt", "value", "7"]);
        result.ParseResult.Errors.Count.ShouldBe(expected: 0);
        result.Run();

        MethodCommandLog.LastOption.ShouldBe("value");
        MethodCommandLog.LastArgument.ShouldBe(expected: 7);
        MethodCommandLog.LastDirective.ShouldBe("dir");
        MethodCommandLog.LastServiceValue.ShouldBe("service");
        MethodCommandLog.ContextSeen.ShouldBeTrue();
    }

    [Fact]
    public async Task Invoke_MethodCommand_ReceivesCancellationToken()
    {
        MethodCommandLog.Reset();
        var settings = new CommandRuntimeSettings { ShowHelpOnEmptyCommand = false };
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("service"));
        var provider = services.BuildServiceProvider();
        var resolver = new ServiceProviderResolver(provider);
        var app = CommandRuntime.Factory.Object.Create<MethodInvocationCommand>(settings, resolver);
        var result = app.Parse(["[trace:dir]", "invoke", "--opt", "value", "7"]);
        result.ParseResult.Errors.Count.ShouldBe(expected: 0);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

#pragma warning disable xUnit1051
        await result.RunAsync(config: null, cts.Token);
#pragma warning restore xUnit1051

        MethodCommandLog.TokenCanceled.ShouldBeTrue();
    }

    [Fact]
    public void Invoke_MethodCommand_UsesSameInstance()
    {
        MethodCommandLog.Reset();
        var settings = new CommandRuntimeSettings { ShowHelpOnEmptyCommand = false };
        var app = CommandRuntime.Factory.Object.Create<MethodInstanceCommand>(settings, serviceResolver: null);
        var result = app.Parse(["--name", "value", "child"]);
        result.ParseResult.Errors.Count.ShouldBe(expected: 0);

        result.Run();

        var instance = result.Bind<MethodInstanceCommand>();
        ReferenceEquals(instance, MethodCommandLog.LastInstance).ShouldBeTrue();
        MethodCommandLog.LastInstanceName.ShouldBe("value");
    }

    [Fact]
    public void Build_MethodContextNotFirst_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<MethodContextNotFirstCommand>();
        var model = CommandModelBuilder.BuildFromObject(shape, shape.Provider);

        Should.Throw<InvalidOperationException>(() => CommandRuntimeBuilder.Build(model, new CommandRuntimeSettings()));
    }

    [Fact]
    public void Build_MethodTokenNotLast_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<MethodTokenNotLastCommand>();
        var model = CommandModelBuilder.BuildFromObject(shape, shape.Provider);

        Should.Throw<InvalidOperationException>(() => CommandRuntimeBuilder.Build(model, new CommandRuntimeSettings()));
    }

    [Fact]
    public void Build_MethodSpecOnContext_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<MethodSpecOnContextCommand>();
        var model = CommandModelBuilder.BuildFromObject(shape, shape.Provider);

        Should.Throw<InvalidOperationException>(() => CommandRuntimeBuilder.Build(model, new CommandRuntimeSettings()));
    }

    [Fact]
    public void Build_MethodSpecOnToken_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<MethodSpecOnTokenCommand>();
        var model = CommandModelBuilder.BuildFromObject(shape, shape.Provider);

        Should.Throw<InvalidOperationException>(() => CommandRuntimeBuilder.Build(model, new CommandRuntimeSettings()));
    }
}