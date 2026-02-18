using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Model;

public class FunctionCommandBuilderTests
{
    [Fact]
    public void Build_FunctionRoot_Succeeds()
    {
        var functionShape = (IFunctionTypeShape)TypeShapeResolver
            .ResolveDynamicOrThrow<FunctionRootCommand, FunctionWitness>();
        var provider = functionShape.Provider;

        var model = CommandModelBuilder.BuildFromFunction(functionShape, provider);

        model.DefinitionType.ShouldBe(typeof(FunctionRootCommand));
        model.Graph.RootNode.ShouldBeOfType<CommandFunctionNode>();
    }

    [Fact]
    public void Build_FunctionRootWithParent_ThrowsByDefault()
    {
        var functionShape = (IFunctionTypeShape)TypeShapeResolver
            .ResolveDynamicOrThrow<FunctionParentedRootCommand, FunctionWitness>();
        var provider = functionShape.Provider;

        Should.Throw<InvalidOperationException>(() => CommandModelBuilder.BuildFromFunction(functionShape, provider));
    }

    [Fact]
    public void Build_FunctionRootWithParent_IgnoresWhenConfigured()
    {
        var functionShape = (IFunctionTypeShape)TypeShapeResolver
            .ResolveDynamicOrThrow<FunctionParentedRootCommand, FunctionWitness>();
        var provider = functionShape.Provider;
        var options = new CommandModelBuildOptions { RootParentHandling = RootParentHandling.Ignore };

        var model = CommandModelBuilder.BuildFromFunction(functionShape, provider, options);

        model.DefinitionType.ShouldBe(typeof(FunctionParentedRootCommand));
        model.Graph.RootNode.Parent.ShouldBeNull();
    }

    [Fact]
    public void Build_FunctionChild_IsLinked()
    {
        var provider = TypeShapeResolver.ResolveDynamicOrThrow<FunctionRootCommand, FunctionWitness>().Provider;
        var shape = (IObjectTypeShape)provider.GetTypeShape(typeof(FunctionParentCommand))!;

        var model = CommandModelBuilder.BuildFromObject(shape, shape.Provider);
        var root = (CommandObjectNode)model.Graph.RootNode;

        root.Children.OfType<CommandFunctionNode>()
            .ShouldContain(node => node.FunctionType == typeof(FunctionChildCommand));
    }

    [Fact]
    public void Build_GenericFunction_Throws()
    {
        var functionShape = (IFunctionTypeShape)TypeShapeResolver
            .ResolveDynamicOrThrow<GenericFunctionCommand<int>, FunctionWitness>();
        var provider = functionShape.Provider;

        Should.Throw<InvalidOperationException>(() => CommandModelBuilder.BuildFromFunction(functionShape, provider));
    }
}