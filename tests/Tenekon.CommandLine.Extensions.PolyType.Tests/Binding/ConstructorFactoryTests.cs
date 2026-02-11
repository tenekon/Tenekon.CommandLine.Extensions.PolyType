using Microsoft.Extensions.DependencyInjection;
using PolyType;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Binding;

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
        var creator = constructor!.Accept(factory) as Func<IServiceProvider?, object>;
        creator.ShouldNotBeNull();

        var instance = (ServiceCtorModel)creator!(provider);
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
        var creator = constructor!.Accept(factory) as Func<IServiceProvider?, object>;
        creator.ShouldNotBeNull();

        var instance = (ServiceCtorModel)creator!(null);
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
        var creator = constructor!.Accept(factory) as Func<IServiceProvider?, object>;
        creator.ShouldNotBeNull();

        Should.Throw<InvalidOperationException>(() => creator!(null));
    }

    [Fact]
    public void CommandLineApp_Create_WhenRequiredCtorMissing_Throws()
    {
        Should.Throw<InvalidOperationException>(() => CommandLineApp.CreateFromType<RequiredCtorParamCommand>(
            new CommandLineSettings(),
            serviceProvider: null));
    }

    [Fact]
    public void CommandLineApp_Bind_OptionalCtor_DefaultsToNull()
    {
        var app = CommandLineApp.CreateFromType<OptionalCtorParamCommand>(new CommandLineSettings(), serviceProvider: null);
        var result = app.Parse([]);
        var instance = result.Bind<OptionalCtorParamCommand>();

        instance.Service.ShouldBeNull();
    }

    [Fact]
    public void CommandLineApp_Bind_WithServiceProvider_ResolvesRequiredCtor()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new RequiredService("value"));
        var provider = services.BuildServiceProvider();

        var app = CommandLineApp.CreateFromType<RequiredCtorParamCommand>(new CommandLineSettings(), provider);
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
