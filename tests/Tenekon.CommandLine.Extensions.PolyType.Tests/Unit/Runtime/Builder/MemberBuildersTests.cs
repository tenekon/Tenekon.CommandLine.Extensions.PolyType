using System.CommandLine;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;
using ArgumentArity = System.CommandLine.ArgumentArity;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime.Builder;

public class MemberBuildersTests
{
    [Fact]
    public void OptionMemberBuilder_ConfiguredSpec_SetsOptionProperties()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<OptionSpecCommand>();
        var property = shape.Properties.First(p => p.Name == nameof(OptionSpecCommand.Values));
        var spec = property.AttributeProvider.GetCustomAttribute<OptionSpecAttribute>()!;
        var namer = TestNamingPolicy.CreateDefault();

        var builder = new OptionMemberBuilder(property, property, spec, namer, new PhysicalFileSystem());
        var result = builder.Build();

        result.ShouldNotBeNull();
        var option = (Option<string[]>)result!.Symbol;

        option.Description.ShouldBe("desc");
        option.Hidden.ShouldBeTrue();
        option.HelpName.ShouldBe("VAL");
        option.AllowMultipleArgumentsPerToken.ShouldBeTrue();
        option.Arity.ShouldBe(ArgumentArity.OneOrMore);
        option.Required.ShouldBeTrue();
        option.Recursive.ShouldBeTrue();
        option.Aliases.ShouldContain("-c");
        option.Aliases.ShouldContain("--c2");
    }

    [Fact]
    public void OptionMemberBuilder_ParseResult_BindsValue()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<OptionDefaultCommand>();
        var property = shape.Properties.First(p => p.Name == nameof(OptionDefaultCommand.Name));
        var spec = property.AttributeProvider.GetCustomAttribute<OptionSpecAttribute>()!;
        var namer = TestNamingPolicy.CreateDefault();

        var builder = new OptionMemberBuilder(property, property, spec, namer, new PhysicalFileSystem());
        var result = builder.Build();

        var command = new RootCommand();
        command.Add((Option)result!.Symbol);
        var parseResult = command.Parse(["--name", "value"]);

        var instance = new OptionDefaultCommand();
        result!.Binder(instance, parseResult);

        instance.Name.ShouldBe("value");
    }

    [Fact]
    public void OptionMemberBuilder_MissingValue_UsesDefault()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<OptionDefaultCommand>();
        var property = shape.Properties.First(p => p.Name == nameof(OptionDefaultCommand.Name));
        var spec = property.AttributeProvider.GetCustomAttribute<OptionSpecAttribute>()!;
        var namer = TestNamingPolicy.CreateDefault();

        var builder = new OptionMemberBuilder(property, property, spec, namer, new PhysicalFileSystem());
        var result = builder.Build();

        RootCommand command = [(Option)result!.Symbol];
        var parseResult = command.Parse([]);

        var instance = new OptionDefaultCommand();
        result!.Binder(instance, parseResult);

        instance.Name.ShouldBe("default");
    }

    [Fact]
    public void OptionMemberBuilder_AllowedValues_InvalidValueProducesError()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<ValidationCommand>();
        var property = shape.Properties.First(p => p.Name == nameof(ValidationCommand.Option));
        var spec = property.AttributeProvider.GetCustomAttribute<OptionSpecAttribute>()!;
        var namer = TestNamingPolicy.CreateDefault();

        var builder = new OptionMemberBuilder(property, property, spec, namer, new PhysicalFileSystem());
        var result = builder.Build();

        RootCommand command = [(Option)result!.Symbol];
        var parseResult = command.Parse(["--option", "C"]);

        parseResult.Errors.Count.ShouldBeGreaterThan(expected: 0);
    }

    [Fact]
    public void ArgumentMemberBuilder_ConfiguredSpec_SetsArgumentProperties()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<ArgumentSpecCommand>();
        var property = shape.Properties.First(p => p.Name == nameof(ArgumentSpecCommand.Value));
        var spec = property.AttributeProvider.GetCustomAttribute<ArgumentSpecAttribute>()!;
        var namer = TestNamingPolicy.CreateDefault();

        var builder = new ArgumentMemberBuilder(property, property, spec, namer, new PhysicalFileSystem());
        var result = builder.Build();

        var argument = (Argument<string>)result!.Symbol;
        argument.Description.ShouldBe("desc");
        argument.HelpName.ShouldBe("ARG");
        argument.Arity.ShouldBe(ArgumentArity.ExactlyOne);
    }

    [Fact]
    public void ArgumentMemberBuilder_ParseResult_BindsValue()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<ArgumentSpecCommand>();
        var property = shape.Properties.First(p => p.Name == nameof(ArgumentSpecCommand.Value));
        var spec = property.AttributeProvider.GetCustomAttribute<ArgumentSpecAttribute>()!;
        var namer = TestNamingPolicy.CreateDefault();

        var builder = new ArgumentMemberBuilder(property, property, spec, namer, new PhysicalFileSystem());
        var result = builder.Build();

        RootCommand command = [(Argument)result!.Symbol];
        var parseResult = command.Parse(["value"]);

        var instance = new ArgumentSpecCommand();
        result!.Binder(instance, parseResult);

        instance.Value.ShouldBe("value");
    }

    [Fact]
    public void ArgumentMemberBuilder_RequiredEnumerable_SetsOneOrMoreArity()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<ArgumentEnumerableCommand>();
        var property = shape.Properties.First(p => p.Name == nameof(ArgumentEnumerableCommand.Items));
        var spec = property.AttributeProvider.GetCustomAttribute<ArgumentSpecAttribute>()!;
        var namer = TestNamingPolicy.CreateDefault();

        var builder = new ArgumentMemberBuilder(property, property, spec, namer, new PhysicalFileSystem());
        var result = builder.Build();

        var argument = (Argument<string[]>)result!.Symbol;
        argument.Arity.ShouldBe(ArgumentArity.OneOrMore);
    }

    [Fact]
    public void ArgumentMemberBuilder_AllowedValues_InvalidValueProducesError()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<ValidationCommand>();
        var property = shape.Properties.First(p => p.Name == nameof(ValidationCommand.Argument));
        var spec = property.AttributeProvider.GetCustomAttribute<ArgumentSpecAttribute>()!;
        var namer = TestNamingPolicy.CreateDefault();

        var builder = new ArgumentMemberBuilder(property, property, spec, namer, new PhysicalFileSystem());
        var result = builder.Build();

        RootCommand command = [(Argument)result!.Symbol];
        var parseResult = command.Parse(["3"]);

        parseResult.Errors.Count.ShouldBeGreaterThan(expected: 0);
    }

    [Fact]
    public void DirectiveMemberBuilder_ParseResult_BindsValues()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<DirectiveCommand>();
        var namer = TestNamingPolicy.CreateDefault();
        var command = new RootCommand();

        var debugProperty = shape.Properties.First(p => p.Name == nameof(DirectiveCommand.Debug));
        var debugSpec = debugProperty.AttributeProvider.GetCustomAttribute<DirectiveSpecAttribute>()!;
        var debugBuilder = new DirectiveMemberBuilder(debugProperty, debugSpec, namer);
        var debugResult = debugBuilder.Build();
        command.Add(debugResult!.Directive);

        var traceProperty = shape.Properties.First(p => p.Name == nameof(DirectiveCommand.Trace));
        var traceSpec = traceProperty.AttributeProvider.GetCustomAttribute<DirectiveSpecAttribute>()!;
        var traceBuilder = new DirectiveMemberBuilder(traceProperty, traceSpec, namer);
        var traceResult = traceBuilder.Build();
        command.Add(traceResult!.Directive);

        var tagsProperty = shape.Properties.First(p => p.Name == nameof(DirectiveCommand.Tags));
        var tagsSpec = tagsProperty.AttributeProvider.GetCustomAttribute<DirectiveSpecAttribute>()!;
        var tagsBuilder = new DirectiveMemberBuilder(tagsProperty, tagsSpec, namer);
        var tagsResult = tagsBuilder.Build();
        command.Add(tagsResult!.Directive);

        var parseResult = command.Parse(["[debug]", "[trace:value]", "[tags:one]", "[tags:two]"]);

        var instance = new DirectiveCommand();
        debugResult!.Binder(instance, parseResult);
        traceResult!.Binder(instance, parseResult);
        tagsResult!.Binder(instance, parseResult);

        instance.Debug.ShouldBeTrue();
        instance.Trace.ShouldBe("value");
        instance.Tags.ShouldBe(["one", "two"]);
    }
}