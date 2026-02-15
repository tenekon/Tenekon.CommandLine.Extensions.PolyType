using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using PolyType;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime.Binding;

public partial class ConstructorFactoryTests
{
    [Fact]
    public void ConstructorFactory_ServiceProvider_ResolvesDependency()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Dep("service"));
        var provider = services.BuildServiceProvider();

        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<ServiceCtorModel>();
        shape.Constructor.ShouldNotBeNull();
        var constructor = shape.Constructor!;

        var factory = new ConstructorFactory();
        var creator =
            constructor!.Accept(factory) as
                Func<BindingContext, ParseResult, ICommandServiceResolver?, CancellationToken, object>;
        creator.ShouldNotBeNull();

        var resolver = new ServiceProviderResolver(provider);
        var bindingContext = new BindingContext(new BindingRegistry(), new CommandRuntimeSettings());
        var parseResult = new RootCommand().Parse([]);
        var instance = (ServiceCtorModel)creator!(bindingContext, parseResult, resolver, CancellationToken.None);
        instance.Dependency.Value.ShouldBe("service");
        instance.Count.ShouldBe(expected: 3);
    }

    [Fact]
    public void ConstructorFactory_NoProvider_UsesDefaultValues()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<ServiceCtorModel>();
        shape.Constructor.ShouldNotBeNull();
        var constructor = shape.Constructor!;

        var factory = new ConstructorFactory();
        var creator =
            constructor!.Accept(factory) as
                Func<BindingContext, ParseResult, ICommandServiceResolver?, CancellationToken, object>;
        creator.ShouldNotBeNull();

        var bindingContext = new BindingContext(new BindingRegistry(), new CommandRuntimeSettings());
        var parseResult = new RootCommand().Parse([]);
        var instance = (ServiceCtorModel)creator!(bindingContext, parseResult, arg3: null, CancellationToken.None);
        instance.Dependency.ShouldBeNull();
        instance.Count.ShouldBe(expected: 3);
    }

    [Fact]
    public void ConstructorFactory_MissingRequiredDependency_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<RequiredDepCtorModel>();
        shape.Constructor.ShouldNotBeNull();
        var constructor = shape.Constructor!;

        var factory = new ConstructorFactory();
        var creator =
            constructor!.Accept(factory) as
                Func<BindingContext, ParseResult, ICommandServiceResolver?, CancellationToken, object>;
        creator.ShouldNotBeNull();

        var bindingContext = new BindingContext(new BindingRegistry(), new CommandRuntimeSettings());
        var parseResult = new RootCommand().Parse([]);
        Should.Throw<InvalidOperationException>(() => creator!(
            bindingContext,
            parseResult,
            arg3: null,
            CancellationToken.None));
    }

    [Fact]
    public void CommandRuntime_Create_WhenRequiredCtorMissing_Throws()
    {
        var app = CommandRuntime.Factory.Object.Create<RequiredCtorParamCommand>(
            new CommandRuntimeSettings(),
            serviceResolver: null);
        var result = app.Parse([]);

        Should.Throw<InvalidOperationException>(() => result.Bind<RequiredCtorParamCommand>());
    }

    [Fact]
    public void CommandRuntime_Bind_OptionalCtor_DefaultsToNull()
    {
        var app = CommandRuntime.Factory.Object.Create<OptionalCtorParamCommand>(
            new CommandRuntimeSettings(),
            serviceResolver: null);
        var result = app.Parse([]);
        var instance = result.Bind<OptionalCtorParamCommand>();

        instance.Service.ShouldBeNull();
    }

    [Fact]
    public void CommandRuntime_Bind_WithServiceProvider_ResolvesRequiredCtor()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new RequiredService("value"));
        var provider = services.BuildServiceProvider();

        var resolver = new ServiceProviderResolver(provider);
        var app = CommandRuntime.Factory.Object.Create<RequiredCtorParamCommand>(
            new CommandRuntimeSettings(),
            resolver);
        var result = app.Parse([]);
        var instance = result.Bind<RequiredCtorParamCommand>();

        instance.Service.Value.ShouldBe("value");
    }

    [GenerateShape]
    public partial class ServiceCtorModel(Dep? dependency = null, int count = 3)
    {
        public Dep? Dependency { get; } = dependency;
        public int Count { get; } = count;
    }

    [GenerateShape]
    public partial class RequiredDepCtorModel(Dep dependency)
    {
        public Dep Dependency { get; } = dependency;
    }

    public sealed class Dep(string value)
    {
        public string Value { get; } = value;
    }
}