using PublicApiGenerator;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.PublicApi;

public sealed class PublicApiTests
{
    [Fact]
    public Task PublicApi_HasNoChanges()
    {
        var publicApi = typeof(CommandRuntime).Assembly.GeneratePublicApi(
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