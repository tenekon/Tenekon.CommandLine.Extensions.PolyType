using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime;

[Collection("HandlerLog")]
public class CommandRuntimeTests
{
    [Fact]
    public void Parse_ProvidedArgs_BindsValues()
    {
        var fixture = new CommandRuntimeFixture();
        var app = fixture.CreateApp<BasicRootCommand>();
        var optionName = TestNamingPolicy.CreateDefault().GetOptionName(nameof(BasicRootCommand.Option1));

        var result = app.Parse([optionName, "value", "argument"]);

        result.ParseResult.Tokens.Count.ShouldBeGreaterThan(expected: 0);
        result.Bind<BasicRootCommand>().Option1.ShouldBe("value");
        result.Bind<BasicRootCommand>().Argument1.ShouldBe("argument");
    }

    [Fact]
    public void Parse_PosixBundlingSetting_AppliesBehavior()
    {
        var enabledFixture = new CommandRuntimeFixture(settings => settings.EnablePosixBundling = true);
        var enabledApp = enabledFixture.CreateApp<BundlingCommand>();
        var enabledResult = enabledApp.Parse(["-ab"]);

        enabledResult.ParseResult.Errors.Count.ShouldBe(expected: 0);
        enabledResult.Bind<BundlingCommand>().A.ShouldBeTrue();
        enabledResult.Bind<BundlingCommand>().B.ShouldBeTrue();

        var disabledFixture = new CommandRuntimeFixture(settings => settings.EnablePosixBundling = false);
        var disabledApp = disabledFixture.CreateApp<BundlingCommand>();
        var disabledResult = disabledApp.Parse(["-ab"]);

        disabledResult.ParseResult.Errors.Count.ShouldBeGreaterThan(expected: 0);
    }

    [Fact]
    public void Parse_ResponseFileTokenReplacer_ReplacesTokens()
    {
        var fixture = new CommandRuntimeFixture(settings =>
        {
            settings.ResponseFileTokenReplacer = (token, out replacement, out errorMessage) =>
            {
                if (string.Equals(token, "repl", StringComparison.Ordinal)
                    || string.Equals(token, "@repl", StringComparison.Ordinal))
                {
                    replacement = ["--value"];
                    errorMessage = null;
                    return true;
                }

                replacement = null;
                errorMessage = null;
                return false;
            };
        });

        var app = fixture.CreateApp<ResponseFileCommand>();
        var result = app.Parse(["@repl", "42"]);

        result.Bind<ResponseFileCommand>().Value.ShouldBe("42");
    }

    [Fact]
    public async Task RunAsync_CancellationToken_Propagates()
    {
        HandlerLog.Reset();
        var settings = new CommandRuntimeSettings { ShowHelpOnEmptyCommand = false };
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("service"));
        var provider = services.BuildServiceProvider();
        var resolver = new ServiceProviderResolver(provider);
        var app = CommandRuntime.Factory.Object.Create<RunAsyncWithServiceAndTokenCommand>(settings, resolver);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

#pragma warning disable xUnit1051
        await app.RunAsync(["--trigger"], cts.Token);
#pragma warning restore xUnit1051

        HandlerLog.LastTokenCanceled.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_UsesProvidedServiceProvider()
    {
        HandlerLog.Reset();
        var settings = new CommandRuntimeSettings { ShowHelpOnEmptyCommand = false };
        var app = CommandRuntime.Factory.Object.Create<RunAsyncWithServiceAndTokenCommand>(
            settings,
            serviceResolver: null);
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("per-run"));
        var provider = services.BuildServiceProvider();
        var resolver = new ServiceProviderResolver(provider);
        var config = new CommandInvocationOptions { ServiceResolver = resolver };

#pragma warning disable xUnit1051
        await app.RunAsync(["--trigger"], config);
#pragma warning restore xUnit1051

        HandlerLog.LastServiceValue.ShouldBe("per-run");
    }
}