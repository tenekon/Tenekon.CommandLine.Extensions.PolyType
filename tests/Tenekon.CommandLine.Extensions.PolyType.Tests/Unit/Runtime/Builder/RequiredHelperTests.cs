using System.CommandLine;
using PolyType;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime.Builder;

public class RequiredHelperTests
{
    [Theory]
    [CombinatorialData]
    public void IsRequired_DerivesRequirednessFromDefinition(
        [CombinatorialMemberData(nameof(Cases))] RequiredCase testCase)
    {
        var option = testCase.Build();

        option.Required.ShouldBe(testCase.ExpectedRequired);
    }

    public static IEnumerable<RequiredCase> Cases =>
    [
        new(
            "NonNullableReference",
            () => BuildOption<RequiredOptionCommand>(nameof(RequiredOptionCommand.RequiredOption)),
            ExpectedRequired: true),
        new(
            "DefaultProvided",
            () => BuildOption<OptionalOptionCommand>(nameof(OptionalOptionCommand.Option)),
            ExpectedRequired: true),
        new(
            "ValueType",
            () => BuildOption<ValueTypeOptionCommand>(nameof(ValueTypeOptionCommand.Count)),
            ExpectedRequired: true),
        new(
            "NullableReference",
            () => BuildOption<NullableOptionCommand>(nameof(NullableOptionCommand.Option)),
            ExpectedRequired: false),
        new(
            "ExplicitRequired",
            () => BuildOption<ExplicitRequiredOptionCommand>(nameof(ExplicitRequiredOptionCommand.Option)),
            ExpectedRequired: true),
        new(
            "ExplicitRequiredValueType",
            () => BuildOption<ExplicitRequiredValueTypeOptionCommand>(
                nameof(ExplicitRequiredValueTypeOptionCommand.Count)),
            ExpectedRequired: true)
    ];

    private static Option BuildOption<TCommand>(string propertyName) where TCommand : IShapeable<TCommand>
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<TCommand>();
        var property = shape.Properties.First(p => p.Name == propertyName);
        var spec = OptionSpecModel.FromAttribute(property.AttributeProvider.GetCustomAttribute<OptionSpecAttribute>()!);
        var namer = TestNamingPolicy.CreateDefault();
        var builder = new OptionMemberBuilder(property, property, spec, namer, new PhysicalFileSystem());
        var result = builder.Build();
        return (Option)result!.Symbol;
    }

    public sealed record RequiredCase(string Name, Func<Option> Build, bool ExpectedRequired);
}
