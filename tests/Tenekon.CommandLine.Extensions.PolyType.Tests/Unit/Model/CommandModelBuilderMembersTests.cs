using PolyType;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Model.Builder;
using BuilderMemberCommand = Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels.Builder.BuilderMemberCommand;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Model;

public class CommandModelBuilderMembersTests
{
    [Fact]
    public void AddMembers_BindsValues()
    {
        var builder = CreateBuilder<BuilderMemberCommand>();
        var root = (CommandObjectModelBuilderNode)builder.Root!;

        var optionProperty = root.Shape.Properties.First(p => p.Name == nameof(BuilderMemberCommand.OptionValue));
        var argumentProperty = root.Shape.Properties.First(p => p.Name == nameof(BuilderMemberCommand.ArgumentValue));
        var directiveProperty = root.Shape.Properties.First(p => p.Name == nameof(BuilderMemberCommand.DirectiveValue));

        root.AddOption(optionProperty, OptionSpecModel.FromAttribute(new OptionSpecAttribute { Name = "opt" }));
        root.AddArgument(argumentProperty, ArgumentSpecModel.FromAttribute(new ArgumentSpecAttribute { Name = "arg" }));
        root.AddDirective(
            directiveProperty,
            DirectiveSpecModel.FromAttribute(new DirectiveSpecAttribute { Name = "trace" }));

        var runtime = CommandRuntime.Factory.CreateFromModel(
            builder.Build(),
            new CommandRuntimeSettings { ShowHelpOnEmptyCommand = false },
            serviceResolver: null);

        var result = runtime.Parse(["[trace:dir]", "--opt", "value", "argument"]);
        var instance = result.Bind<BuilderMemberCommand>();

        instance.OptionValue.ShouldBe("value");
        instance.ArgumentValue.ShouldBe("argument");
        instance.DirectiveValue.ShouldBe("dir");
    }

    [Fact]
    public void ReplaceMember_UpdatesSymbol()
    {
        var builder = CreateBuilder<BuilderMemberCommand>();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var optionProperty = root.Shape.Properties.First(p => p.Name == nameof(BuilderMemberCommand.OptionValue));

        root.ReplaceMember(
            optionProperty,
            OptionSpecModel.FromAttribute(new OptionSpecAttribute { Name = "first" }),
            argument: null,
            directive: null);
        root.ReplaceMember(
            optionProperty,
            OptionSpecModel.FromAttribute(new OptionSpecAttribute { Name = "second" }),
            argument: null,
            directive: null);

        var runtime = CommandRuntime.Factory.CreateFromModel(
            builder.Build(),
            new CommandRuntimeSettings { ShowHelpOnEmptyCommand = false },
            serviceResolver: null);

        var parseResult = runtime.Parse(["--second", "value"]);
        parseResult.ParseResult.Errors.Count.ShouldBe(expected: 0);

        var instance = parseResult.Bind<BuilderMemberCommand>();
        instance.OptionValue.ShouldBe("value");

        var failed = runtime.Parse(["--first", "value"]);
        failed.ParseResult.Errors.Count.ShouldBeGreaterThan(expected: 0);
    }

    [Fact]
    public void RemoveMember_RemovesSymbol()
    {
        var builder = CreateBuilder<BuilderMemberCommand>();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var optionProperty = root.Shape.Properties.First(p => p.Name == nameof(BuilderMemberCommand.OptionValue));

        root.AddOption(optionProperty, OptionSpecModel.FromAttribute(new OptionSpecAttribute { Name = "opt" }));
        root.RemoveMember(optionProperty).ShouldBeTrue();

        var runtime = CommandRuntime.Factory.CreateFromModel(
            builder.Build(),
            new CommandRuntimeSettings { ShowHelpOnEmptyCommand = false },
            serviceResolver: null);

        var parseResult = runtime.Parse(["--opt", "value"]);
        parseResult.ParseResult.Errors.Count.ShouldBeGreaterThan(expected: 0);
    }

    private static CommandModelBuilder CreateBuilder<TCommand>() where TCommand : IShapeable<TCommand>
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<TCommand>();
        var model = CommandModelFactory.BuildFromObject(shape, shape.Provider);
        return model.ToBuilder();
    }
}