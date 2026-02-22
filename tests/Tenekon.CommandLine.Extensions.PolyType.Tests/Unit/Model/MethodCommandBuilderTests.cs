using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Model;

public class MethodCommandBuilderTests
{
    [Fact]
    public void Build_MethodCommands_AreChildren()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<MethodRootCommand>();

        var definition = CommandModelFactory.BuildFromObject(shape, shape.Provider);
        var root = (CommandObjectNode)definition.Graph.RootNode;

        root.MethodChildren.Count.ShouldBe(expected: 1);
        var methodNode = root.MethodChildren[index: 0];
        methodNode.MethodShape.Name.ShouldBe("RunChild");

        var children = methodNode.Children.OfType<CommandObjectNode>().ToList();
        children.Count.ShouldBe(expected: 1);
        children[index: 0].DefinitionType.ShouldBe(typeof(MethodChildCommand));
        children[index: 0].Parent.ShouldBe(methodNode);
    }

    [Fact]
    public void Build_StaticMethodCommand_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<StaticMethodCommand>();

        Should.Throw<InvalidOperationException>(() => CommandModelFactory.BuildFromObject(shape, shape.Provider));
    }

    [Fact]
    public void Build_MethodParentMismatch_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<InvalidMethodParentCommand>();

        Should.Throw<InvalidOperationException>(() => CommandModelFactory.BuildFromObject(shape, shape.Provider));
    }

    [Fact]
    public void Build_OverloadMissingName_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<OverloadMissingNameCommand>();

        Should.Throw<InvalidOperationException>(() => CommandModelFactory.BuildFromObject(shape, shape.Provider));
    }

    [Fact]
    public void Build_OverloadNamedOk_Succeeds()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<OverloadNamedOkCommand>();

        var model = CommandModelFactory.BuildFromObject(shape, shape.Provider);

        var root = (CommandObjectNode)model.Graph.RootNode;
        root.MethodChildren.Count.ShouldBe(expected: 2);
    }

    [Fact]
    public void Build_InterfaceMethodCommand_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<InterfaceMethodCommand>();

        Should.Throw<InvalidOperationException>(() => CommandModelFactory.BuildFromObject(shape, shape.Provider));
    }

    [Fact]
    public void Build_GenericMethodCommand_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<GenericMethodCommand>();

        Should.Throw<InvalidOperationException>(() => CommandModelFactory.BuildFromObject(shape, shape.Provider));
    }

    [Fact]
    public void Build_GenericDeclaringTypeCommand_Throws()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<GenericBaseDerivedCommand>();

        Should.Throw<InvalidOperationException>(() => CommandModelFactory.BuildFromObject(shape, shape.Provider));
    }
}