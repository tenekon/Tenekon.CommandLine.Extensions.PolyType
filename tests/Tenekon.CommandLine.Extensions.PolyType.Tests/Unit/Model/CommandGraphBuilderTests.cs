using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Model;

public class CommandGraphBuilderTests
{
    [Fact]
    public void Build_MissingCommandSpec_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<MissingCommandSpec>();

        Should.Throw<InvalidOperationException>(() => CommandModelBuilder.BuildFromObject(shape, shape.Provider));
    }

    [Fact]
    public void Build_NestedCommands_LinkedToParent()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<RootWithChildrenCommand>();

        var definition = CommandModelBuilder.BuildFromObject(shape, shape.Provider);
        var root = definition.Graph.RootNode;

        var children = root.Children.OfType<CommandObjectNode>().ToList();
        children.Count.ShouldBe(expected: 2);
        children.ShouldContain(child => child.DefinitionType == typeof(RootWithChildrenCommand.ChildACommand));
        children.ShouldContain(child => child.DefinitionType == typeof(RootWithChildrenCommand.ChildBCommand));
    }

    [Fact]
    public void Build_ConflictingParents_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<ConflictingParentRoot>();

        Should.Throw<InvalidOperationException>(() => CommandModelBuilder.BuildFromObject(shape, shape.Provider));
    }

    [Fact]
    public void Build_CycleDetected_Throws()
    {
        var shapeA = (IObjectTypeShape)TypeShapeResolver.Resolve<CycleA>();

        Should.Throw<InvalidOperationException>(() => CommandModelBuilder.BuildFromObject(shapeA, shapeA.Provider));
    }

    [Fact]
    public void Build_OptionNameCollisionAcrossHierarchy_AllowsDuplicates()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<CollisionRootCommand>();

        var definition = CommandModelBuilder.BuildFromObject(shape, shape.Provider);
        var runtime = CommandRuntimeBuilder.Build(definition, new CommandRuntimeSettings());

        runtime.Graph.RootCommand.ShouldNotBeNull();
    }

    [Fact]
    public void Build_OptionAliasCollisionAcrossHierarchy_AllowsDuplicates()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<AliasCollisionRootCommand>();

        var definition = CommandModelBuilder.BuildFromObject(shape, shape.Provider);
        var runtime = CommandRuntimeBuilder.Build(definition, new CommandRuntimeSettings());

        runtime.Graph.RootCommand.ShouldNotBeNull();
    }
}
