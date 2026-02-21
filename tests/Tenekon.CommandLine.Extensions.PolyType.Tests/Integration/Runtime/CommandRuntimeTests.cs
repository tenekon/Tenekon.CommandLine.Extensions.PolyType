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
    public void Parse_ResponseFileTokenReplacer_Error_ReturnsParseError()
    {
        var fixture = new CommandRuntimeFixture(settings =>
        {
            settings.ResponseFileTokenReplacer = (token, out replacement, out errorMessage) =>
            {
                replacement = null;
                errorMessage = "token-error";
                return false;
            };
        });

        var app = fixture.CreateApp<ResponseFileCommand>();
        var result = app.Parse(["@bad"]);

        result.ParseResult.Errors.Count.ShouldBeGreaterThan(expected: 0);
        result.ParseResult.Errors.Any(error => error.Message.Contains("token-error", StringComparison.Ordinal))
            .ShouldBeTrue();
    }

    [Fact]
    public void Parse_TreatUnmatchedTokensAsErrors_ReportsError()
    {
        var fixture = new CommandRuntimeFixture();
        var app = fixture.CreateApp<UnmatchedTokensErrorCommand>();
        var result = app.Parse(["extra"]);

        result.ParseResult.Errors.Count.ShouldBeGreaterThan(expected: 0);
    }

    [Fact]
    public void Parse_TreatUnmatchedTokensAsErrors_Disabled_AllowsTokens()
    {
        var fixture = new CommandRuntimeFixture();
        var app = fixture.CreateApp<UnmatchedTokensAllowedCommand>();
        var result = app.Parse(["extra"]);

        result.ParseResult.Errors.Count.ShouldBe(expected: 0);
    }

    [Fact]
    public void Run_EmptyArgs_ShowHelpOnEmptyCommand_SuppressesHandler()
    {
        HandlerLog.Reset();
        var fixture = new CommandRuntimeFixture(settings => settings.ShowHelpOnEmptyCommand = true);
        var app = fixture.CreateApp<RunCommand>();

        var code = app.Run([]);

        code.ShouldBe(expected: 0);
        HandlerLog.RunCount.ShouldBe(expected: 0);
        fixture.Output.ToString().ToLowerInvariant().ShouldContain("usage");
    }

    [Fact]
    public async Task RunAsync_EmptyArgs_ShowHelpOnEmptyCommand_SuppressesHandler()
    {
        HandlerLog.Reset();
        var fixture = new CommandRuntimeFixture(settings => settings.ShowHelpOnEmptyCommand = true);
        var app = fixture.CreateApp<RunAsyncCommand>();

        var code = await app.RunAsync([]);

        code.ShouldBe(expected: 0);
        HandlerLog.RunAsyncCount.ShouldBe(expected: 0);
        fixture.Output.ToString().ToLowerInvariant().ShouldContain("usage");
    }

    [Fact]
    public void Run_EmptyArgs_ShowHelpOnEmptyCommandDisabled_InvokesHandler()
    {
        HandlerLog.Reset();
        var fixture = new CommandRuntimeFixture(settings => settings.ShowHelpOnEmptyCommand = false);
        var app = fixture.CreateApp<RunCommand>();

        app.Run([]);

        HandlerLog.RunCount.ShouldBe(expected: 1);
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
