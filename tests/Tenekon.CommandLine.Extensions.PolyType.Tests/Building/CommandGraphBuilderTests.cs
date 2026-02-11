using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Building;

public class CommandGraphBuilderTests
{
    [Fact]
    public void Build_MissingCommandSpec_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<MissingCommandSpec>();

        Should.Throw<InvalidOperationException>(() => CommandGraphBuilder.Build(
            shape,
            shape.Provider,
            new CommandLineSettings(),
            serviceProvider: null));
    }

    [Fact]
    public void Build_NestedCommands_LinkedToParent()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<RootWithChildrenCommand>();

        var graph = CommandGraphBuilder.Build(shape, shape.Provider, new CommandLineSettings(), serviceProvider: null);
        var root = graph.RootDescriptor;

        root.Children.Count.ShouldBe(expected: 2);
        root.Children.ShouldContain(child => child.DefinitionType == typeof(RootWithChildrenCommand.ChildACommand));
        root.Children.ShouldContain(child => child.DefinitionType == typeof(RootWithChildrenCommand.ChildBCommand));
    }

    [Fact]
    public void Build_ConflictingParents_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<ConflictingParentRoot>();

        Should.Throw<InvalidOperationException>(() => CommandGraphBuilder.Build(
            shape,
            shape.Provider,
            new CommandLineSettings(),
            serviceProvider: null));
    }

    [Fact]
    public void ValidateNoCycles_CycleDetected_Throws()
    {
        var shapeA = (IObjectTypeShape)TypeShapeResolver.Resolve<CycleA>();
        var shapeB = (IObjectTypeShape)TypeShapeResolver.Resolve<CycleB>();
        var specA = shapeA.AttributeProvider.GetCustomAttribute<CommandSpecAttribute>()!;
        var specB = shapeB.AttributeProvider.GetCustomAttribute<CommandSpecAttribute>()!;

        var descriptorA = new CommandDescriptor(typeof(CycleA), shapeA, specA);
        var descriptorB = new CommandDescriptor(typeof(CycleB), shapeB, specB);

        descriptorA.Children.Add(descriptorB);
        descriptorB.Children.Add(descriptorA);

        Should.Throw<InvalidOperationException>(() => descriptorA.ValidateNoCycles());
    }

    [Fact]
    public void Build_OptionNameCollisionAcrossHierarchy_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<CollisionRootCommand>();

        Should.Throw<InvalidOperationException>(() => CommandGraphBuilder.Build(
            shape,
            shape.Provider,
            new CommandLineSettings(),
            serviceProvider: null));
    }

    [Fact]
    public void Build_OptionAliasCollisionAcrossHierarchy_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<AliasCollisionRootCommand>();

        Should.Throw<InvalidOperationException>(() => CommandGraphBuilder.Build(
            shape,
            shape.Provider,
            new CommandLineSettings(),
            serviceProvider: null));
    }
}