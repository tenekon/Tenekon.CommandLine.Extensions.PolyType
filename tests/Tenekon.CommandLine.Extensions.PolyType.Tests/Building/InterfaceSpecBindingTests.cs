using PolyType;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Building;

public class InterfaceSpecBindingTests
{
    [Fact]
    public void Build_InterfaceSpecOnClassAndInterface_Throws()
    {
        var settings = new CommandLineSettings();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<InterfaceSpecConflictCommand>();

        Should.Throw<InvalidOperationException>(() =>
            CommandGraphBuilder.Build(shape, shape.Provider, settings, serviceProvider: null));
    }

    [Fact]
    public void Build_MultipleInterfaceSpecsForSameProperty_Throws()
    {
        var settings = new CommandLineSettings();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<InterfaceSpecMultipleConflictCommand>();

        Should.Throw<InvalidOperationException>(() =>
            CommandGraphBuilder.Build(shape, shape.Provider, settings, serviceProvider: null));
    }

    [Fact]
    public void Build_InterfaceOptionNameCollisionAcrossInterfaces_Throws()
    {
        var settings = new CommandLineSettings();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<InterfaceSpecAliasCollisionCommand>();

        Should.Throw<InvalidOperationException>(() =>
            CommandGraphBuilder.Build(shape, shape.Provider, settings, serviceProvider: null));
    }

    [Fact]
    public void Build_BinderMapIncludesInterfaceTargets_RegistersEntries()
    {
        var settings = new CommandLineSettings();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<InterfaceSpecCommand>();
        var graph = CommandGraphBuilder.Build(shape, shape.Provider, settings, serviceProvider: null);

        graph.BindingContext.BinderMap.ContainsKey(new BinderKey(typeof(InterfaceSpecCommand), typeof(InterfaceSpecCommand)))
            .ShouldBeTrue();
        graph.BindingContext.BinderMap.ContainsKey(new BinderKey(typeof(InterfaceSpecCommand), typeof(IInterfaceSpecOption)))
            .ShouldBeTrue();
        graph.BindingContext.BinderMap.ContainsKey(new BinderKey(typeof(InterfaceSpecCommand), typeof(IInterfaceSpecArgument)))
            .ShouldBeTrue();
    }

    [Fact]
    public void Build_InheritanceUsesNewSymbolPool_CreatesDistinctOptions()
    {
        var settings = new CommandLineSettings();
        var baseShape = (IObjectTypeShape)TypeShapeResolver.Resolve<InterfaceSpecBaseCommand>();
        var baseGraph = CommandGraphBuilder.Build(baseShape, baseShape.Provider, settings, serviceProvider: null);
        var derivedShape = (IObjectTypeShape)TypeShapeResolver.Resolve<InterfaceSpecDerivedCommand>();
        var derivedGraph = CommandGraphBuilder.Build(derivedShape, derivedShape.Provider, settings, serviceProvider: null);

        static bool IsCustomOption(System.CommandLine.Option option)
            => !option.Aliases.Any(alias =>
                alias.Contains("help", StringComparison.OrdinalIgnoreCase)
                || alias.Contains("version", StringComparison.OrdinalIgnoreCase));

        var baseOptions = baseGraph.RootCommand.Options.Where(IsCustomOption).ToArray();
        var derivedOptions = derivedGraph.RootCommand.Options.Where(IsCustomOption).ToArray();

        baseOptions.Length.ShouldBeGreaterThan(0);
        derivedOptions.Length.ShouldBeGreaterThan(0);

        var baseOption = baseOptions[0];
        var derivedOption = derivedOptions[0];

        ReferenceEquals(baseOption, derivedOption).ShouldBeFalse();
    }

    [Fact]
    public void Bind_InterfaceTargetOnlyOptionSpec_DoesNotRequireOtherInterfaces()
    {
        var fixture = new CommandAppFixture();
        var result = fixture.Parse<InterfaceSpecCommand>(["--iface-option", "value", "argument"]);
        var target = new InterfaceSpecOptionTarget();

        result.Bind<InterfaceSpecCommand, IInterfaceSpecOption>(target);

        target.OptionValue.ShouldBe("value");
    }
}
