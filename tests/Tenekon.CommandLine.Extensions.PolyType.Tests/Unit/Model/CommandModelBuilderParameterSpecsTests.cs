using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Model.Builder;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels.Builder;
using BuilderFunctionWitness = Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels.Builder.BuilderFunctionWitness;
using BuilderMethodCommand = Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels.Builder.BuilderMethodCommand;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Model;

public class CommandModelBuilderParameterSpecsTests
{
    [Fact]
    public void MethodParameters_SetSpecs_BindsValues()
    {
        BuilderMethodLog.Reset();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<BuilderMethodCommand>();
        var model = CommandModelFactory.BuildFromObject(shape, shape.Provider);
        var builder = model.ToBuilder();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var methodNode = root.MethodChildren.Single();

        var optionParam = methodNode.MethodShape.Parameters[index: 0];
        var argumentParam = methodNode.MethodShape.Parameters[index: 1];

        methodNode.SetOption(optionParam, OptionSpecModel.FromAttribute(new OptionSpecAttribute { Name = "opt" }));
        methodNode.SetArgument(
            argumentParam,
            ArgumentSpecModel.FromAttribute(new ArgumentSpecAttribute { Name = "arg" }));

        var runtime = CommandRuntime.Factory.CreateFromModel(
            builder.Build(),
            new CommandRuntimeSettings { ShowHelpOnEmptyCommand = false },
            serviceResolver: null);

        var result = runtime.Parse(["invoke", "--opt", "value", "7"]);
        result.Run();

        BuilderMethodLog.LastOption.ShouldBe("value");
        BuilderMethodLog.LastArgument.ShouldBe(expected: 7);
    }

    [Fact]
    public void FunctionParameters_SetSpecs_BindsValues()
    {
        BuilderFunctionLog.Reset();
        var provider = TypeShapeResolver.ResolveDynamicOrThrow<BuilderFunctionCommand, BuilderFunctionWitness>()
            .Provider;
        var functionShape = provider.GetTypeShape(typeof(BuilderFunctionCommand)) as IFunctionTypeShape
            ?? throw new InvalidOperationException("Missing function shape.");
        var model = CommandModelFactory.BuildFromFunction(functionShape, provider);
        var builder = model.ToBuilder();
        var root = (CommandFunctionModelBuilderNode)builder.Root!;

        var optionParam = root.FunctionShape.Parameters[index: 0];
        var argumentParam = root.FunctionShape.Parameters[index: 1];

        root.SetOption(optionParam, OptionSpecModel.FromAttribute(new OptionSpecAttribute { Name = "opt" }));
        root.SetArgument(argumentParam, ArgumentSpecModel.FromAttribute(new ArgumentSpecAttribute { Name = "arg" }));

        var runtime = CommandRuntime.Factory.CreateFromModel(
            builder.Build(),
            new CommandRuntimeSettings { ShowHelpOnEmptyCommand = false },
            serviceResolver: null);

        runtime.FunctionRegistry.Set<BuilderFunctionCommand>((optionValue, argumentValue) =>
        {
            BuilderFunctionLog.LastOption = optionValue;
            BuilderFunctionLog.LastArgument = argumentValue;
        });

        var result = runtime.Parse(["--opt", "value", "7"]);
        result.Run();

        BuilderFunctionLog.LastOption.ShouldBe("value");
        BuilderFunctionLog.LastArgument.ShouldBe(expected: 7);
    }
}