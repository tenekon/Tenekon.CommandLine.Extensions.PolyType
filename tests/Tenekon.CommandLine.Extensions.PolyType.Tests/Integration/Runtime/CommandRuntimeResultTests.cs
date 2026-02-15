using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime;

[Collection("HandlerLog")]
public class CommandRuntimeResultTests
{
    [Fact]
    public void TryGetCalledType_CalledChildCommand_ReturnsInvokedCommand()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<RootWithChildrenCommand>(["child-b"]);

        result.TryGetCalledType(out var type).ShouldBeTrue();
        type.ShouldBe(typeof(RootWithChildrenCommand.ChildBCommand));
    }

    [Fact]
    public void TryBindCalled_CalledChildCommand_ReturnsInstance()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<RootWithChildrenCommand>(["child-a"]);

        result.TryBindCalled(out var value).ShouldBeTrue();
        value.ShouldBeOfType<RootWithChildrenCommand.ChildACommand>();
    }

    [Fact]
    public void IsCalled_RootCommandParsed_ReturnsTrue()
    {
        var fixture = new CommandRuntimeFixture();
        var optionName = TestNamingPolicy.CreateDefault().GetOptionName(nameof(BasicRootCommand.Option1));
        var result = fixture.Parse<BasicRootCommand>([optionName, "value", "argument"]);

        result.IsCalled<BasicRootCommand>().ShouldBeTrue();
    }

    [Fact]
    public void Run_HelpRequested_WritesToSettingsOutput()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<BasicRootCommand>(["--help"]);

        result.Run();
        fixture.Output.ToString().ShouldContain("Basic root command");
    }

    [Fact]
    public void Run_HandlerReturnsInt_ReturnsExitCode()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<RunReturnsIntCommand>(["--trigger"]);

        var exitCode = result.Run();

        exitCode.ShouldBe(expected: 7);
    }

    [Fact]
    public async Task RunAsync_HandlerReturnsInt_ReturnsExitCode()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<RunAsyncReturnsIntCommand>(["--trigger"]);

        var exitCode = await result.RunAsync();

        exitCode.ShouldBe(expected: 5);
    }

    [Fact]
    public async Task RunAsync_CancellationToken_Propagates()
    {
        HandlerLog.Reset();
        var fixture = new CommandRuntimeFixture();
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("service"));
        var provider = services.BuildServiceProvider();
        var app = fixture.CreateApp<RunAsyncWithServiceAndTokenCommand>(provider);
        var result = app.Parse(["--trigger"]);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

#pragma warning disable xUnit1051
        await result.RunAsync(cts.Token);
#pragma warning restore xUnit1051

        HandlerLog.LastTokenCanceled.ShouldBeTrue();
    }

    [Fact]
    public void Run_UsesProvidedServiceProvider()
    {
        HandlerLog.Reset();
        var fixture = new CommandRuntimeFixture();
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("per-run"));
        var provider = services.BuildServiceProvider();
        var app = fixture.CreateApp<RunWithServiceCommand>(serviceProvider: null);
        var result = app.Parse(["--trigger"]);
        var resolver = new ServiceProviderResolver(provider);
        var config = new CommandInvocationOptions { ServiceResolver = resolver };

        result.Run(config);

        HandlerLog.LastServiceValue.ShouldBe("per-run");
    }

    [Fact]
    public async Task RunAsync_UsesProvidedServiceProvider()
    {
        HandlerLog.Reset();
        var fixture = new CommandRuntimeFixture();
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("per-run"));
        var provider = services.BuildServiceProvider();
        var app = fixture.CreateApp<RunAsyncWithServiceAndTokenCommand>(serviceProvider: null);
        var result = app.Parse(["--trigger"]);
        var resolver = new ServiceProviderResolver(provider);
        var config = new CommandInvocationOptions { ServiceResolver = resolver };

#pragma warning disable xUnit1051
        await result.RunAsync(config);
#pragma warning restore xUnit1051

        HandlerLog.LastServiceValue.ShouldBe("per-run");
    }
}