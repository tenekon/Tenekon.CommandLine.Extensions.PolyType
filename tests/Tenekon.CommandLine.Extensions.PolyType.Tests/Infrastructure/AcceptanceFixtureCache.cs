namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;

internal sealed class AcceptanceFixtureCache
{
    private static readonly Lazy<AcceptanceFixture> s_instance = new(() => new AcceptanceFixture());

    public static AcceptanceFixture Instance => s_instance.Value;
}