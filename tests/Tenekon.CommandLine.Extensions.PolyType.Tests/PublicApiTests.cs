using PublicApiGenerator;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests;

public sealed class PublicApiTests
{
    [Fact]
    public Task PublicApi_HasNoChanges()
    {
        var publicApi = typeof(CommandLineApp).Assembly.GeneratePublicApi(
            new ApiGeneratorOptions
            {
                ExcludeAttributes =
                [
                    "System.Runtime.CompilerServices.InternalsVisibleToAttribute",
                    "System.Reflection.AssemblyMetadataAttribute"
                ]
            });

        return Verify(publicApi);
    }
}
