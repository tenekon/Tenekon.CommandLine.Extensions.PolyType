using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Execution;

[Collection("HandlerLog")]
public class CommandLineAppTests
{
    [Fact]
    public void Parse_ProvidedArgs_BindsValues()
    {
        var fixture = new CommandAppFixture();
        var app = fixture.CreateApp<BasicRootCommand>();
        var namer = new CommandLineNamer(
            nameAutoGenerate: null,
            nameCasingConvention: null,
            namePrefixConvention: null,
            shortFormAutoGenerate: null,
            shortFormPrefixConvention: null);
        var optionName = namer.GetOptionName(nameof(BasicRootCommand.Option1));

        var result = app.Parse([optionName, "value", "argument"]);

        result.ParseResult.Tokens.Count.ShouldBeGreaterThan(expected: 0);
        result.Bind<BasicRootCommand>().Option1.ShouldBe("value");
        result.Bind<BasicRootCommand>().Argument1.ShouldBe("argument");
    }

    [Fact]
    public void Parse_PosixBundlingSetting_AppliesBehavior()
    {
        var enabledFixture = new CommandAppFixture(settings => settings.EnablePosixBundling = true);
        var enabledApp = enabledFixture.CreateApp<BundlingCommand>();
        var enabledResult = enabledApp.Parse(["-ab"]);

        enabledResult.ParseResult.Errors.Count.ShouldBe(expected: 0);
        enabledResult.Bind<BundlingCommand>().A.ShouldBeTrue();
        enabledResult.Bind<BundlingCommand>().B.ShouldBeTrue();

        var disabledFixture = new CommandAppFixture(settings => settings.EnablePosixBundling = false);
        var disabledApp = disabledFixture.CreateApp<BundlingCommand>();
        var disabledResult = disabledApp.Parse(["-ab"]);

        disabledResult.ParseResult.Errors.Count.ShouldBeGreaterThan(expected: 0);
    }

    [Fact]
    public void Parse_ResponseFileTokenReplacer_ReplacesTokens()
    {
        var fixture = new CommandAppFixture(settings =>
        {
            settings.ResponseFileTokenReplacer = (
                string token,
                out IReadOnlyList<string>? replacement,
                out string? errorMessage) =>
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
        var settings = new CommandLineSettings { ShowHelpOnEmptyCommand = false };
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("service"));
        var provider = services.BuildServiceProvider();
        var app = CommandLineApp.CreateFromType<RunAsyncWithServiceAndTokenCommand>(settings, provider);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

#pragma warning disable xUnit1051
        await app.RunAsync(["--trigger"], cts.Token);
#pragma warning restore xUnit1051

        HandlerLog.LastTokenCanceled.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_UsesProvidedServiceProvider()
    {
        HandlerLog.Reset();
        var settings = new CommandLineSettings { ShowHelpOnEmptyCommand = false };
        var app = CommandLineApp.CreateFromType<RunAsyncWithServiceAndTokenCommand>(settings, serviceProvider: null);
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("per-run"));
        var provider = services.BuildServiceProvider();
        var config = new CommandInvocationConfiguration { ServiceProvider = provider };

#pragma warning disable xUnit1051
        await app.RunAsync(["--trigger"], config);
#pragma warning restore xUnit1051

        HandlerLog.LastServiceValue.ShouldBe("per-run");
    }
}
