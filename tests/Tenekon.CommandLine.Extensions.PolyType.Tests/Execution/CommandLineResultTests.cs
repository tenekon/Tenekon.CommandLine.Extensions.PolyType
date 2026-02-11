using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Execution;

[Collection("HandlerLog")]
public class CommandLineResultTests
{
    [Fact]
    public void TryGetCalledType_CalledChildCommand_ReturnsInvokedCommand()
    {
        var fixture = new CommandAppFixture();
        var result = fixture.Parse<RootWithChildrenCommand>(["child-b"]);

        result.TryGetCalledType(out var type).ShouldBeTrue();
        type.ShouldBe(typeof(RootWithChildrenCommand.ChildBCommand));
    }

    [Fact]
    public void TryBindCalled_CalledChildCommand_ReturnsInstance()
    {
        var fixture = new CommandAppFixture();
        var result = fixture.Parse<RootWithChildrenCommand>(["child-a"]);

        result.TryBindCalled(out var value).ShouldBeTrue();
        value.ShouldBeOfType<RootWithChildrenCommand.ChildACommand>();
    }

    [Fact]
    public void IsCalled_RootCommandParsed_ReturnsTrue()
    {
        var fixture = new CommandAppFixture();
        var namer = new CommandLineNamer(
            nameAutoGenerate: null,
            nameCasingConvention: null,
            namePrefixConvention: null,
            shortFormAutoGenerate: null,
            shortFormPrefixConvention: null);
        var optionName = namer.GetOptionName(nameof(BasicRootCommand.Option1));
        var result = fixture.Parse<BasicRootCommand>([optionName, "value", "argument"]);

        result.IsCalled<BasicRootCommand>().ShouldBeTrue();
    }

    [Fact]
    public void Run_HelpRequested_WritesToSettingsOutput()
    {
        var fixture = new CommandAppFixture();
        var result = fixture.Parse<BasicRootCommand>(["--help"]);

        result.Run();
        fixture.Output.ToString().ShouldContain("Basic root command");
    }

    [Fact]
    public void Run_HandlerReturnsInt_ReturnsExitCode()
    {
        var fixture = new CommandAppFixture();
        var result = fixture.Parse<RunReturnsIntCommand>(["--trigger"]);

        var exitCode = result.Run();

        exitCode.ShouldBe(7);
    }

    [Fact]
    public async Task RunAsync_HandlerReturnsInt_ReturnsExitCode()
    {
        var fixture = new CommandAppFixture();
        var result = fixture.Parse<RunAsyncReturnsIntCommand>(["--trigger"]);

        var exitCode = await result.RunAsync(TestContext.Current.CancellationToken);

        exitCode.ShouldBe(5);
    }

    [Fact]
    public async Task RunAsync_CancellationToken_Propagates()
    {
        HandlerLog.Reset();
        var fixture = new CommandAppFixture();
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("service"));
        var provider = services.BuildServiceProvider();
        var app = fixture.CreateApp<RunAsyncWithServiceAndTokenCommand>(provider);
        var result = app.Parse(["--trigger"]);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

#pragma warning disable xUnit1051
        await result.RunAsync(cts.Token);
#pragma warning restore xUnit1051

        HandlerLog.LastTokenCanceled.ShouldBeTrue();
    }

    [Fact]
    public void Run_UsesProvidedServiceProvider()
    {
        HandlerLog.Reset();
        var fixture = new CommandAppFixture();
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("per-run"));
        var provider = services.BuildServiceProvider();
        var app = fixture.CreateApp<RunWithServiceCommand>(serviceProvider: null);
        var result = app.Parse(["--trigger"]);
        var config = new CommandInvocationConfiguration { ServiceProvider = provider };

        result.Run(config);

        HandlerLog.LastServiceValue.ShouldBe("per-run");
    }

    [Fact]
    public async Task RunAsync_UsesProvidedServiceProvider()
    {
        HandlerLog.Reset();
        var fixture = new CommandAppFixture();
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("per-run"));
        var provider = services.BuildServiceProvider();
        var app = fixture.CreateApp<RunAsyncWithServiceAndTokenCommand>(serviceProvider: null);
        var result = app.Parse(["--trigger"]);
        var config = new CommandInvocationConfiguration { ServiceProvider = provider };

#pragma warning disable xUnit1051
        await result.RunAsync(config, TestContext.Current.CancellationToken);
#pragma warning restore xUnit1051

        HandlerLog.LastServiceValue.ShouldBe("per-run");
    }
}
