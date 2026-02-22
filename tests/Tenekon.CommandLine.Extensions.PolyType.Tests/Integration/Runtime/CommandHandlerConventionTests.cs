using System.Collections.Immutable;
using PolyType;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Model.Builder;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime;

[Collection("HandlerLog")]
public class CommandHandlerConventionTests
{
    [Theory]
    [InlineData("ExecuteAsync", "Execute", true)]
    [InlineData("Execute", "ExecuteAsync", false)]
    public void Convention_ExecuteAndExecuteAsync_RespectsOrder(string first, string second, bool expectAsync)
    {
        HandlerLog.Reset();
        var runtime = CreateRuntime<ExecuteAndExecuteAsyncCommand>([first, second], preferAsync: true, disabled: false);

        var code = runtime.Run(["--trigger"]);

        code.ShouldBe(expected: 0);
        if (expectAsync)
        {
            HandlerLog.RunAsyncCount.ShouldBe(expected: 1);
            HandlerLog.RunCount.ShouldBe(expected: 0);
        }
        else
        {
            HandlerLog.RunCount.ShouldBe(expected: 1);
            HandlerLog.RunAsyncCount.ShouldBe(expected: 0);
        }
    }

    [Theory]
    [InlineData("Execute")]
    [InlineData("ExecuteAsync")]
    public void Convention_ExecuteSingleName_UsesThatHandler(string methodName)
    {
        HandlerLog.Reset();
        var runtime = CreateRuntime<ExecuteAndExecuteAsyncCommand>([methodName], preferAsync: true, disabled: false);

        var code = runtime.Run(["--trigger"]);

        code.ShouldBe(expected: 0);
        if (methodName == "ExecuteAsync")
        {
            HandlerLog.RunAsyncCount.ShouldBe(expected: 1);
            HandlerLog.RunCount.ShouldBe(expected: 0);
        }
        else
        {
            HandlerLog.RunCount.ShouldBe(expected: 1);
            HandlerLog.RunAsyncCount.ShouldBe(expected: 0);
        }
    }

    [Fact]
    public void Convention_Disabled_SkipsHandlerInvocation()
    {
        HandlerLog.Reset();
        var runtime = CreateRuntime<ExecuteAndExecuteAsyncCommand>(
            ["ExecuteAsync", "Execute"],
            preferAsync: true,
            disabled: true);

        var code = runtime.Run(["--trigger"]);

        code.ShouldBe(expected: 0);
        HandlerLog.RunAsyncCount.ShouldBe(expected: 0);
        HandlerLog.RunCount.ShouldBe(expected: 0);
    }

    private static CommandRuntime CreateRuntime<TCommand>(
        ImmutableArray<string> MethodNames,
        bool preferAsync,
        bool disabled) where TCommand : IShapeable<TCommand>
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<TCommand>();
        var model = CommandModelFactory.BuildFromObject(shape, shape.Provider);
        var builder = model.ToBuilder();
        var root = (CommandObjectModelBuilderNode)builder.Root!;

        root.HandlerConvention.MethodNames = MethodNames;
        root.HandlerConvention.PreferAsync = preferAsync;
        root.HandlerConvention.Disabled = disabled;

        var runtime = CommandRuntime.Factory.CreateFromModel(
            builder.Build(),
            new CommandRuntimeSettings
            {
                ShowHelpOnEmptyCommand = false
            },
            serviceResolver: null);
        return runtime;
    }
}