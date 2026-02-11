using System.CommandLine;
using PolyType;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Building;

public class RequiredHelperTests
{
    [Fact]
    public void IsRequired_NonNullableReference_ReturnsFalse()
    {
        var option = BuildOption(nameof(RequiredOptionCommand.RequiredOption), new RequiredOptionCommand());

        option.Required.ShouldBeFalse();
    }

    [Fact]
    public void IsRequired_DefaultProvided_ReturnsFalse()
    {
        var option = BuildOption(nameof(OptionalOptionCommand.Option), new OptionalOptionCommand());

        option.Required.ShouldBeFalse();
    }

    [Fact]
    public void IsRequired_ValueType_ReturnsFalse()
    {
        var option = BuildOption(nameof(ValueTypeOptionCommand.Count), new ValueTypeOptionCommand());

        option.Required.ShouldBeFalse();
    }

    [Fact]
    public void IsRequired_NullableReference_ReturnsFalse()
    {
        var option = BuildOption(nameof(NullableOptionCommand.Option), new NullableOptionCommand());

        option.Required.ShouldBeFalse();
    }

    [Fact]
    public void IsRequired_ExplicitRequired_OverridesNullability()
    {
        var option = BuildOption(nameof(ExplicitRequiredOptionCommand.Option), new ExplicitRequiredOptionCommand());

        option.Required.ShouldBeTrue();
    }

    [Fact]
    public void IsRequired_ExplicitRequired_ValueType_ReturnsTrue()
    {
        var option = BuildOption(
            nameof(ExplicitRequiredValueTypeOptionCommand.Count),
            new ExplicitRequiredValueTypeOptionCommand());

        option.Required.ShouldBeTrue();
    }

    private static Option BuildOption<TCommand>(string propertyName, TCommand defaultInstance)
        where TCommand : IShapeable<TCommand>
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<TCommand>();
        var property = shape.Properties.First(p => p.Name == propertyName);
        var spec = property.AttributeProvider.GetCustomAttribute<OptionSpecAttribute>()!;
        var namer = new CommandLineNamer(
            nameAutoGenerate: null,
            nameCasingConvention: null,
            namePrefixConvention: null,
            shortFormAutoGenerate: null,
            shortFormPrefixConvention: null);
        var builder = new OptionMemberBuilder(property, property, spec, namer, defaultInstance!);
        var result = builder.Build();
        return (Option)result!.Symbol;
    }
}
