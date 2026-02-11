using System.Collections;
using System.CommandLine;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Building;

public class CommandDescriptorTests
{
    [Fact]
    public void Build_RootCommand_IncludesHelpAndVersionOptions()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<BasicRootCommand>();
        var graph = CommandGraphBuilder.Build(shape, shape.Provider, new CommandLineSettings(), serviceProvider: null);

        var rootCommand = graph.RootCommand;

        rootCommand.Options.Any(option => option.GetType().Name == "HelpOption").ShouldBeTrue();
        rootCommand.Options.Any(option => option.GetType().Name == "VersionOption").ShouldBeTrue();
    }

    [Fact]
    public void Build_Subcommands_AddedInOrder()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<RootWithChildrenCommand>();
        var graph = CommandGraphBuilder.Build(shape, shape.Provider, new CommandLineSettings(), serviceProvider: null);

        var subcommands = graph.RootCommand.Subcommands.ToArray();

        subcommands.Length.ShouldBe(expected: 2);
        subcommands[0].Name.ShouldBe("child-a");
        subcommands[1].Name.ShouldBe("child-b");
    }

    [Fact]
    public void Build_SpecMembers_IncludeOptionsAndArguments()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<BasicRootCommand>();
        var graph = CommandGraphBuilder.Build(shape, shape.Provider, new CommandLineSettings(), serviceProvider: null);

        var descriptor = graph.RootDescriptor;

        descriptor.SpecMembers.Any(member => member.DisplayName == nameof(BasicRootCommand.Option1)).ShouldBeTrue();
        descriptor.SpecMembers.Any(member => member.DisplayName == nameof(BasicRootCommand.Argument1)).ShouldBeTrue();
    }

    [Fact]
    public void Build_ParentAccessors_IncludeAncestorTypes()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<RootWithChildrenCommand>();
        var graph = CommandGraphBuilder.Build(shape, shape.Provider, new CommandLineSettings(), serviceProvider: null);

        var childDescriptor = graph.RootDescriptor.Children.First(d => d.DefinitionType
            == typeof(RootWithChildrenCommand.ChildACommand));

        childDescriptor.ParentAccessors.Count.ShouldBe(expected: 1);
        childDescriptor.ParentAccessors[index: 0].ParentType.ShouldBe(typeof(RootWithChildrenCommand));
    }

    [Fact]
    public void DisplayName_CommandBuilt_UsesCommandName()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<BasicRootCommand>();
        var graph = CommandGraphBuilder.Build(shape, shape.Provider, new CommandLineSettings(), serviceProvider: null);

        graph.RootDescriptor.DisplayName.ShouldBe(graph.RootDescriptor.Command!.Name);
    }

    [Fact]
    public void Build_BuiltInDirectives_RespectSettings()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<BasicRootCommand>();
        var enabledSettings = new CommandLineSettings
        {
            EnableSuggestDirective = true,
            EnableDiagramDirective = true,
            EnableEnvironmentVariablesDirective = true
        };
        var enabledGraph = CommandGraphBuilder.Build(shape, shape.Provider, enabledSettings, serviceProvider: null);
        var enabledNames = GetDirectiveNames(enabledGraph.RootCommand);

        enabledNames.ShouldContain(new System.CommandLine.Completions.SuggestDirective().Name);
        enabledNames.ShouldContain(new DiagramDirective().Name);
        enabledNames.ShouldContain(new EnvironmentVariablesDirective().Name);

        var disabledSettings = new CommandLineSettings
        {
            EnableSuggestDirective = false,
            EnableDiagramDirective = false,
            EnableEnvironmentVariablesDirective = false
        };
        var disabledGraph = CommandGraphBuilder.Build(shape, shape.Provider, disabledSettings, serviceProvider: null);
        var disabledNames = GetDirectiveNames(disabledGraph.RootCommand);

        disabledNames.ShouldNotContain(new DiagramDirective().Name);
        disabledNames.ShouldNotContain(new EnvironmentVariablesDirective().Name);
    }

    private static IReadOnlyList<string> GetDirectiveNames(RootCommand rootCommand)
    {
        var directivesProperty = rootCommand.GetType().GetProperty("Directives");
        if (directivesProperty?.GetValue(rootCommand) is not IEnumerable directives)
            return [];

        var names = new List<string>();
        foreach (var directive in directives)
        {
            if (directive is null) continue;
            var nameProperty = directive.GetType().GetProperty("Name");
            if (nameProperty?.GetValue(directive) is string name)
                names.Add(name);
        }

        return names;
    }
}