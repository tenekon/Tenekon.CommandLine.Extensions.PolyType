using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Model.Builder;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Model;

public class CommandModelBuilderTests
{
    [Fact]
    public void Validate_NoRoot_ReturnsDiagnostic()
    {
        var builder = CommandModelBuilder.CreateEmpty();

        var result = builder.Validate();

        result.IsValid.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0000");
    }

    [Fact]
    public void Validate_DuplicateMember_ReturnsDiagnostic()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<OptionDefaultCommand>();
        var model = CommandModelFactory.BuildFromObject(shape, shape.Provider);
        var builder = model.ToBuilder();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var property = root.Shape.Properties.First(p => p.Name == nameof(OptionDefaultCommand.Name));

        root.AddOption(property, OptionSpecModel.FromAttribute(new OptionSpecAttribute()));

        var result = builder.Validate();

        result.IsValid.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0200");
    }

    [Fact]
    public void Build_FromEmpty_AddObjectCommand_SetsRoot()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<OptionDefaultCommand>();
        var builder = CommandModelBuilder.CreateEmpty();

        builder.AddObjectCommand<OptionDefaultCommand>(shape.Provider);

        var model = builder.Build();

        model.DefinitionType.ShouldBe(typeof(OptionDefaultCommand));
    }

    [Fact]
    public void Build_CustomHandlerConvention_UsesRunBeforeRunAsync()
    {
        HandlerLog.Reset();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<RunAndRunAsyncCommand>();
        var model = CommandModelFactory.BuildFromObject(shape, shape.Provider);
        var builder = model.ToBuilder();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        root.HandlerConvention.MethodNames = ["Run", "RunAsync"];

        var updated = builder.Build();
        var runtime = CommandRuntime.Factory.CreateFromModel(updated, settings: null, serviceResolver: null);
        runtime.Run(["--trigger"]);

        HandlerLog.RunCount.ShouldBe(expected: 1);
        HandlerLog.RunAsyncCount.ShouldBe(expected: 0);
    }

    [Fact]
    public void Build_DisabledHandlerConvention_SkipsHandler()
    {
        HandlerLog.Reset();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<RunCommand>();
        var model = CommandModelFactory.BuildFromObject(shape, shape.Provider);
        var builder = model.ToBuilder();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        root.HandlerConvention.Disabled = true;

        var updated = builder.Build();
        var runtime = CommandRuntime.Factory.CreateFromModel(updated, settings: null, serviceResolver: null);
        runtime.Run(["--trigger"]);

        HandlerLog.RunCount.ShouldBe(expected: 0);
    }
}
