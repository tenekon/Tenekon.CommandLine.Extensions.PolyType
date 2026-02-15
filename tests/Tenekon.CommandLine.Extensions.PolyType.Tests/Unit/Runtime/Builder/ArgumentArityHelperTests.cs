using Shouldly;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime.Builder;

public class ArgumentArityHelperTests
{
    [Theory]
    [CombinatorialData]
    public void Map_UsesExpectedArity([CombinatorialMemberData(nameof(Cases))] ArityCase testCase)
    {
        var mapped = ArgumentArityHelper.Map(testCase.Input);

        mapped.ShouldBe(testCase.Expected);
    }

    public static IEnumerable<ArityCase> Cases =>
    [
        new(ArgumentArity.Zero, System.CommandLine.ArgumentArity.Zero),
        new(ArgumentArity.ZeroOrOne, System.CommandLine.ArgumentArity.ZeroOrOne),
        new(ArgumentArity.ExactlyOne, System.CommandLine.ArgumentArity.ExactlyOne),
        new(ArgumentArity.ZeroOrMore, System.CommandLine.ArgumentArity.ZeroOrMore),
        new(ArgumentArity.OneOrMore, System.CommandLine.ArgumentArity.OneOrMore)
    ];

    public sealed record ArityCase(ArgumentArity Input, System.CommandLine.ArgumentArity Expected);
}