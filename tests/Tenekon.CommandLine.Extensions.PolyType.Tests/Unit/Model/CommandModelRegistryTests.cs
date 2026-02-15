using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Model;

public class CommandModelRegistryTests
{
    [Fact]
    public void GetOrCreateFromProvider_SameKey_ReturnsSameInstance()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<BasicRootCommand>();

        var registry = new CommandModelRegistry();
        var first = registry.Object.GetOrAdd<BasicRootCommand>(shape.Provider);
        var second = registry.Object.GetOrAdd<BasicRootCommand>(shape.Provider);

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void GetOrCreateFromProvider_SameProvider_ReturnsSameInstance()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<BasicRootCommand>();

        var registry = new CommandModelRegistry();
        var first = registry.Object.GetOrAdd<BasicRootCommand>(shape.Provider);
        var second = registry.Object.GetOrAdd<BasicRootCommand>(shape.Provider);

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void GetOrCreateFromProvider_DifferentOptions_Rebuilds()
    {
        var provider = TypeShapeResolver.ResolveDynamicOrThrow<FunctionRootCommand, FunctionWitness>().Provider;
        var registry = new CommandModelRegistry();

        Should.Throw<InvalidOperationException>(() =>
            registry.Function.GetOrAdd<FunctionParentedRootCommand>(provider));

        var options = new CommandModelBuildOptions { RootParentHandling = RootParentHandling.Ignore };
        var model = registry.Function.GetOrAdd<FunctionParentedRootCommand>(provider, options);

        model.DefinitionType.ShouldBe(typeof(FunctionParentedRootCommand));
    }
}