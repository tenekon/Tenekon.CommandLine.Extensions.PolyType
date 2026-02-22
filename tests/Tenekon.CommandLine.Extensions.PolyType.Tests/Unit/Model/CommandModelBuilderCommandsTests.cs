using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Model.Builder;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels.Builder;
using BuilderFunctionWitness = Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels.Builder.BuilderFunctionWitness;
using BuilderMemberCommand = Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels.Builder.BuilderMemberCommand;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Model;

public class CommandModelBuilderCommandsTests
{
    [Fact]
    public void AddObjectCommand_SetsRootAndBuilds()
    {
        var builder = CommandModelBuilder.CreateEmpty();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<BuilderMemberCommand>();
        var node = builder.AddObjectCommand(shape.Type, shape.Provider);
        builder.SetRoot(node);

        var model = builder.Build();
        model.Root.CommandType.ShouldBe(node.CommandType);

        var runtime = CommandRuntime.Factory.CreateFromModel(
            model,
            new CommandRuntimeSettings(),
            serviceResolver: null);
        runtime.Parse([]).ParseResult.ShouldNotBeNull();
    }

    [Fact]
    public void AddFunctionCommand_SetsRootAndBuilds()
    {
        var builder = CommandModelBuilder.CreateEmpty();
        var provider = TypeShapeResolver.ResolveDynamicOrThrow<BuilderFunctionCommand, BuilderFunctionWitness>()
            .Provider;
        var shape = provider.GetTypeShape(typeof(BuilderFunctionCommand)) as IFunctionTypeShape
            ?? throw new InvalidOperationException("Missing function shape.");
        var node = builder.AddFunctionCommand(shape.Type, provider);
        builder.SetRoot(node);

        var model = builder.Build();
        model.Root.CommandType.ShouldBe(node.CommandType);

        var runtime = CommandRuntime.Factory.CreateFromModel(
            model,
            new CommandRuntimeSettings(),
            serviceResolver: null);
        runtime.FunctionRegistry.Set<BuilderFunctionCommand>((_, _) => { });
        runtime.Parse([]).ParseResult.ShouldNotBeNull();
    }

    [Fact]
    public void AddCommand_WithoutSpecAttribute_Throws()
    {
        var builder = CommandModelBuilder.CreateEmpty();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<MissingCommandSpec>();

        Should.Throw<InvalidOperationException>(() => builder.AddObjectCommand(shape.Type, shape.Provider));
    }

    [Fact]
    public void SetRoot_RequiresNodeInBuilder()
    {
        var builder = CommandModelBuilder.CreateEmpty();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<BuilderMemberCommand>();
        var node = builder.AddObjectCommand(shape.Type, shape.Provider);

        var otherBuilder = CommandModelBuilder.CreateEmpty();
        var otherShape = (IObjectTypeShape)TypeShapeResolver.Resolve<RunCommand>();
        var otherNode = otherBuilder.AddObjectCommand(otherShape.Type, otherShape.Provider);

        builder.SetRoot(node);

        Should.Throw<InvalidOperationException>(() => builder.SetRoot(otherNode));
    }

    [Fact]
    public void RemoveNode_RemovesRoot()
    {
        var builder = CommandModelBuilder.CreateEmpty();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<BuilderMemberCommand>();
        var node = builder.AddObjectCommand(shape.Type, shape.Provider);
        builder.SetRoot(node);

        builder.RemoveNode(node);

        var result = builder.Validate();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0000");
    }
}
