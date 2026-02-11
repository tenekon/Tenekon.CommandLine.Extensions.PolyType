using PolyType;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Context;

public class CommandLineContextTests
{
    [Fact]
    public void IsEmptyCommand_NoTokens_ReturnsTrue()
    {
        var (context, _) = CreateContext<BasicRootCommand>([]);

        context.IsEmptyCommand().ShouldBeTrue();
    }

    [Fact]
    public void ShowHierarchy_CommandTree_WritesHierarchy()
    {
        var (context, output) = CreateContext<RootWithChildrenCommand>([]);

        context.ShowHierarchy();

        var text = output.ToString();
        text.ShouldNotBeNullOrWhiteSpace();
        text.ShouldContain("child-a");
        text.ShouldContain("child-b");
    }

    [Fact]
    public void ShowValues_ParsedValues_WritesFormattedValues()
    {
        var namer = new CommandLineNamer(
            nameAutoGenerate: null,
            nameCasingConvention: null,
            namePrefixConvention: null,
            shortFormAutoGenerate: null,
            shortFormPrefixConvention: null);
        var optionName = namer.GetOptionName(nameof(BasicRootCommand.Option1));

        var (context, output) = CreateContext<BasicRootCommand>([optionName, "value", "argument"]);

        context.ShowValues();

        var text = output.ToString();
        text.ShouldContain("Option1 = \"value\"");
        text.ShouldContain("Argument1 = \"argument\"");
    }

    [Fact]
    public void ShowHelp_CommandContext_WritesUsage()
    {
        var (context, output) = CreateContext<BasicRootCommand>([]);

        context.ShowHelp();

        output.ToString().ToLowerInvariant().ShouldContain("usage");
    }

    private static (CommandLineContext Context, StringWriter Output) CreateContext<TCommand>(string[] args)
        where TCommand : IShapeable<TCommand>
    {
        var output = new StringWriter();
        var settings = new CommandLineSettings { Output = output, Error = output };
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<TCommand>();
        var graph = CommandGraphBuilder.Build(shape, shape.Provider, settings, serviceProvider: null);
        var parseResult = graph.RootCommand.Parse(args);
        var context = new CommandLineContext(graph.BindingContext, parseResult, settings, graph.RootDescriptor);
        return (context, output);
    }
}