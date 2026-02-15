using Microsoft.Extensions.DependencyInjection;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime;

[Collection("HandlerLog")]
public class FunctionCommandInvocationTests
{
    [Fact]
    public void Invoke_FunctionCommand_BindsValuesAndServices()
    {
        FunctionCommandLog.Reset();
        var settings = new CommandRuntimeSettings { ShowHelpOnEmptyCommand = false };
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("service"));
        var provider = services.BuildServiceProvider();
        var resolver = new ServiceProviderResolver(provider);
        var shapeProvider = TypeShapeResolver.ResolveDynamicOrThrow<FunctionRootCommand, FunctionWitness>().Provider;
        var app = CommandRuntime.Factory.Function.Create<FunctionRootCommand>(shapeProvider, settings, resolver);

        app.FunctionRegistry.Set<FunctionRootCommand>((context, option, argument, directive, dependency, token) =>
        {
            FunctionCommandLog.LastOption = option;
            FunctionCommandLog.LastArgument = argument;
            FunctionCommandLog.LastDirective = directive;
            FunctionCommandLog.LastServiceValue = dependency.Value;
            FunctionCommandLog.ContextSeen = context is not null;
            FunctionCommandLog.TokenCanceled = token.IsCancellationRequested;
        });

        var result = app.Parse(["[trace:dir]", "--opt", "value", "7"]);
        result.ParseResult.Errors.Count.ShouldBe(expected: 0);
        result.Run();

        FunctionCommandLog.LastOption.ShouldBe("value");
        FunctionCommandLog.LastArgument.ShouldBe(expected: 7);
        FunctionCommandLog.LastDirective.ShouldBe("dir");
        FunctionCommandLog.LastServiceValue.ShouldBe("service");
        FunctionCommandLog.ContextSeen.ShouldBeTrue();
    }

    [Fact]
    public void Invoke_FunctionCommand_OverrideResolver_Wins()
    {
        FunctionCommandLog.Reset();
        var settings = new CommandRuntimeSettings { ShowHelpOnEmptyCommand = false };
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("service"));
        var provider = services.BuildServiceProvider();
        var resolver = new ServiceProviderResolver(provider);
        var shapeProvider = TypeShapeResolver.ResolveDynamicOrThrow<FunctionRootCommand, FunctionWitness>().Provider;
        var app = CommandRuntime.Factory.Function.Create<FunctionRootCommand>(shapeProvider, settings, resolver);

        app.FunctionRegistry.Set<FunctionRootCommand>((_, option, _, _, _, _) =>
        {
            FunctionCommandLog.LastOption = $"registry-{option}";
        });

        var overrideFunction = new FunctionRootCommand((_, option, _, _, _, _) =>
        {
            FunctionCommandLog.LastOption = $"override-{option}";
        });

        var result = app.Parse(["--opt", "value", "7"]);
        result.ParseResult.Errors.Count.ShouldBe(expected: 0);
        result.Run(
            new CommandInvocationOptions
            {
                FunctionResolver = new FixedFunctionResolver<FunctionRootCommand>(overrideFunction)
            });

        FunctionCommandLog.LastOption.ShouldBe("override-value");
    }

    [Fact]
    public void Invoke_FunctionCommand_ServiceFallback_WorksWhenEnabled()
    {
        FunctionCommandLog.Reset();
        var settings = new CommandRuntimeSettings
        {
            ShowHelpOnEmptyCommand = false,
            AllowFunctionResolutionFromServices = true
        };

        var services = new ServiceCollection();
        services.AddSingleton<FunctionRootCommand>((context, option, argument, directive, dependency, token) =>
        {
            FunctionCommandLog.LastOption = option;
            FunctionCommandLog.LastArgument = argument;
            FunctionCommandLog.LastDirective = directive;
            FunctionCommandLog.LastServiceValue = dependency.Value;
            FunctionCommandLog.ContextSeen = context is not null;
            FunctionCommandLog.TokenCanceled = token.IsCancellationRequested;
        });
        services.AddSingleton(new DiDependency("service"));
        var provider = services.BuildServiceProvider();
        var resolver = new ServiceProviderResolver(provider);
        var shapeProvider = TypeShapeResolver.ResolveDynamicOrThrow<FunctionRootCommand, FunctionWitness>().Provider;

        var app = CommandRuntime.Factory.Function.Create<FunctionRootCommand>(shapeProvider, settings, resolver);

        var result = app.Parse(["[trace:dir]", "--opt", "value", "7"]);
        result.ParseResult.Errors.Count.ShouldBe(expected: 0);
        result.Run();

        FunctionCommandLog.LastOption.ShouldBe("value");
        FunctionCommandLog.LastArgument.ShouldBe(expected: 7);
        FunctionCommandLog.LastDirective.ShouldBe("dir");
        FunctionCommandLog.LastServiceValue.ShouldBe("service");
        FunctionCommandLog.ContextSeen.ShouldBeTrue();
    }

    [Fact]
    public void Invoke_FunctionCommand_MissingInstance_Throws()
    {
        var settings = new CommandRuntimeSettings { ShowHelpOnEmptyCommand = false };
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("service"));
        var provider = services.BuildServiceProvider();
        var resolver = new ServiceProviderResolver(provider);
        var shapeProvider = TypeShapeResolver.ResolveDynamicOrThrow<FunctionRootCommand, FunctionWitness>().Provider;
        var app = CommandRuntime.Factory.Function.Create<FunctionRootCommand>(shapeProvider, settings, resolver);

        var result = app.Parse(["--opt", "value", "7"]);
        result.ParseResult.Errors.Count.ShouldBe(expected: 0);

        Should.Throw<InvalidOperationException>(() => result.Run());
    }
}