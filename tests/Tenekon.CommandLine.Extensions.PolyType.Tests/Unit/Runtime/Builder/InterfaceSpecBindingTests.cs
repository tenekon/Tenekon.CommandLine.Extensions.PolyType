using System.CommandLine;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime.Builder;

public class InterfaceSpecBindingTests
{
    [Fact]
    public void Build_InterfaceSpecOnClassAndInterface_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<InterfaceSpecConflictCommand>();

        Should.Throw<InvalidOperationException>(() => CommandModelFactory.BuildFromObject(shape, shape.Provider));
    }

    [Fact]
    public void Build_MultipleInterfaceSpecsForSameProperty_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<InterfaceSpecMultipleConflictCommand>();

        Should.Throw<InvalidOperationException>(() => CommandModelFactory.BuildFromObject(shape, shape.Provider));
    }

    [Fact]
    public void Build_InterfaceOptionNameCollisionAcrossInterfaces_Throws()
    {
        var settings = new CommandRuntimeSettings();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<InterfaceSpecAliasCollisionCommand>();

        Should.Throw<InvalidOperationException>(() =>
        {
            var definition = CommandModelFactory.BuildFromObject(shape, shape.Provider);
            CommandRuntimeBuilder.Build(definition, settings);
        });
    }

    [Fact]
    public void Build_BinderMapIncludesInterfaceTargets_RegistersEntries()
    {
        var settings = new CommandRuntimeSettings();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<InterfaceSpecCommand>();
        var definition = CommandModelFactory.BuildFromObject(shape, shape.Provider);
        var runtime = CommandRuntimeBuilder.Build(definition, settings);
        var bindingContext = runtime.BindingContext;

        bindingContext.BinderMap.ContainsKey(new BinderKey(typeof(InterfaceSpecCommand), typeof(InterfaceSpecCommand)))
            .ShouldBeTrue();
        bindingContext.BinderMap.ContainsKey(new BinderKey(typeof(InterfaceSpecCommand), typeof(IInterfaceSpecOption)))
            .ShouldBeTrue();
        bindingContext.BinderMap
            .ContainsKey(new BinderKey(typeof(InterfaceSpecCommand), typeof(IInterfaceSpecArgument)))
            .ShouldBeTrue();
    }

    [Fact]
    public void Build_InheritanceUsesNewSymbolPool_CreatesDistinctOptions()
    {
        var settings = new CommandRuntimeSettings();
        var baseShape = (IObjectTypeShape)TypeShapeResolver.Resolve<InterfaceSpecBaseCommand>();
        var baseDefinition = CommandModelFactory.BuildFromObject(baseShape, baseShape.Provider);
        var baseGraph = CommandRuntimeBuilder.Build(baseDefinition, settings).Graph;
        var derivedShape = (IObjectTypeShape)TypeShapeResolver.Resolve<InterfaceSpecDerivedCommand>();
        var derivedDefinition = CommandModelFactory.BuildFromObject(derivedShape, derivedShape.Provider);
        var derivedGraph = CommandRuntimeBuilder.Build(derivedDefinition, settings).Graph;

        static bool IsCustomOption(Option option)
        {
            return !option.Aliases.Any(alias => alias.Contains("help", StringComparison.OrdinalIgnoreCase)
                || alias.Contains("version", StringComparison.OrdinalIgnoreCase));
        }

        var baseOptions = baseGraph.RootCommand.Options.Where(IsCustomOption).ToArray();
        var derivedOptions = derivedGraph.RootCommand.Options.Where(IsCustomOption).ToArray();

        baseOptions.Length.ShouldBeGreaterThan(expected: 0);
        derivedOptions.Length.ShouldBeGreaterThan(expected: 0);

        var baseOption = baseOptions[0];
        var derivedOption = derivedOptions[0];

        ReferenceEquals(baseOption, derivedOption).ShouldBeFalse();
    }

    [Fact]
    public void Bind_InterfaceTargetOnlyOptionSpec_DoesNotRequireOtherInterfaces()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<InterfaceSpecCommand>(["--iface-option", "value", "argument"]);
        var target = new InterfaceSpecOptionTarget();

        result.Bind<InterfaceSpecCommand, IInterfaceSpecOption>(target);

        target.OptionValue.ShouldBe("value");
    }
}