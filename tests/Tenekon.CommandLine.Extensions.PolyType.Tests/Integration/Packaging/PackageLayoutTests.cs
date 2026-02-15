using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Packaging;

public sealed class PackageLayoutTests(PackageLayoutFixture fixture) : IClassFixture<PackageLayoutFixture>
{
    [Theory]
    [InlineData("build/Tenekon.CommandLine.Extensions.PolyType.props")]
    [InlineData("buildTransitive/Tenekon.CommandLine.Extensions.PolyType.props")]
    [InlineData("Tenekon.CommandLine.Extensions.PolyType.Common.props")]
    [InlineData("lib/net10.0/Tenekon.CommandLine.Extensions.PolyType.dll")]
    [InlineData("lib/netstandard2.0/Tenekon.CommandLine.Extensions.PolyType.dll")]
    [InlineData("analyzers/dotnet/cs/Tenekon.CommandLine.Extensions.PolyType.SourceGenerator.dll")]
    public void Package_Contains_Expected_Entries(string entry)
    {
        fixture.Entries.ShouldContain(entry);
    }

    [Fact]
    public void Package_LibEntries_AreUnderTargetFrameworks()
    {
        var libEntries = fixture.Entries.Where(e => e.StartsWith("lib/", StringComparison.OrdinalIgnoreCase)).ToArray();
        var allowedPrefixes = new[]
        {
            "lib/net10.0/",
            "lib/netstandard2.0/"
        };

        libEntries.All(entry => allowedPrefixes.Any(prefix => entry.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase)))
            .ShouldBeTrue();
    }
}