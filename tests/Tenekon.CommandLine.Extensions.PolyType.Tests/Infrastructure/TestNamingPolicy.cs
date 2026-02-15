namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;

internal static class TestNamingPolicy
{
    public static CommandNamingPolicy CreateDefault()
    {
        return new CommandNamingPolicy(
            nameAutoGenerate: null,
            nameCasingConvention: null,
            namePrefixConvention: null,
            shortFormAutoGenerate: null,
            shortFormPrefixConvention: null);
    }
}