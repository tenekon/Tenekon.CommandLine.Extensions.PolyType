using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Execution;

[Collection("HandlerLog")]
public class MinimalCommandLineRunnerTests
{
    [Fact]
    public async Task Helper_Chainable_LoopsForwardedArgs()
    {
        HelperLog.Reset();
        var fixture = new MinimalRunnerFixture();

        var exitCode = await fixture.RunAsync<HelperRootCommand>(["--forward"]);

        exitCode.ShouldBe(0);
        HelperLog.RootRuns.ShouldBe(1);
        HelperLog.NextRuns.ShouldBe(1);
        fixture.FirstStageProvider.ShouldNotBeNull();
        fixture.SecondStageProviders.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Helper_CircuitBreaks_OnHelp()
    {
        HelperLog.Reset();
        var fixture = new MinimalRunnerFixture();

        var exitCode = await fixture.RunAsync<HelperRootCommand>(["--help"]);

        exitCode.ShouldBe(0);
        HelperLog.RootRuns.ShouldBe(0);
        fixture.SecondStageProviders.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Helper_CircuitBreaks_OnParseErrors()
    {
        HelperLog.Reset();
        var fixture = new MinimalRunnerFixture();

        await fixture.RunAsync<HelperRootCommand>(["--unknown"]);

        HelperLog.RootRuns.ShouldBe(0);
        fixture.SecondStageProviders.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Helper_Describing_BypassesSecondStageProvider()
    {
        HelperLog.Reset();
        var fixture = new MinimalRunnerFixture();

        var exitCode = await fixture.RunAsync<HelperDescribingCommand>([]);

        exitCode.ShouldBe(0);
        HelperLog.DescribingRuns.ShouldBe(1);
        fixture.SecondStageProviders.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Helper_PreBindHook_Invoked_AndServiceConfigured()
    {
        HelperLog.Reset();
        ConfigurableCommand.BoundValue = null;
        var fixture = new MinimalRunnerFixture();

        var exitCode = await fixture.RunAsync<ConfigurableCommand>(["--name", "configured"]);

        exitCode.ShouldBe(0);
        ConfigurableCommand.BoundValue.ShouldBe("configured");
        HelperLog.ConfiguredValue.ShouldBe("configured");
        fixture.SecondStageProviders.Count.ShouldBe(1);
    }
}
