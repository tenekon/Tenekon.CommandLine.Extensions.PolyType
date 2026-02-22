using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using PolyType;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime;

[Collection("HandlerLog")]
public class CommandHandlerFactoryTests
{
    [Fact]
    public void Resolve_RunCommand_IncludesRunMethod()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<RunCommand>();

        shape.Methods.Any(method => method.Name == "Run").ShouldBeTrue();
    }

    [Fact]
    public void TryCreateHandler_BothRunAndRunAsync_PrefersAsync()
    {
        HandlerLog.Reset();
        var settings = new CommandRuntimeSettings();
        var (handler, parseResult, _) = CreateHandler<RunAndRunAsyncCommand>(["--trigger"], settings);

        handler.Invoke(parseResult, arg2: null);

        HandlerLog.RunAsyncCount.ShouldBe(expected: 1);
        HandlerLog.RunCount.ShouldBe(expected: 0);
    }

    [Fact]
    public void Invoke_RunCommand_InvokesRun()
    {
        HandlerLog.Reset();
        var settings = new CommandRuntimeSettings();
        var (handler, parseResult, _) = CreateHandler<RunCommand>(["--trigger"], settings);

        handler.Invoke(parseResult, arg2: null);

        HandlerLog.RunCount.ShouldBe(expected: 1);
    }

    [Fact]
    public void Invoke_RunReturnsInt_ReturnsCode()
    {
        HandlerLog.Reset();
        var settings = new CommandRuntimeSettings();
        var (handler, parseResult, _) = CreateHandler<RunReturnsIntCommand>(["--trigger"], settings);

        var code = handler.Invoke(parseResult, arg2: null);

        code.ShouldBe(expected: 7);
    }

    [Fact]
    public async Task InvokeAsync_RunAsyncReturnsInt_ReturnsCode()
    {
        HandlerLog.Reset();
        var settings = new CommandRuntimeSettings();
        var (handler, parseResult, _) = CreateHandler<RunAsyncReturnsIntCommand>(["--trigger"], settings);
        var code = await handler.InvokeAsync(parseResult, arg2: null, CancellationToken.None);

        code.ShouldBe(expected: 5);
        HandlerLog.RunAsyncCount.ShouldBe(expected: 1);
    }

    [Fact]
    public async Task InvokeAsync_RunAsyncWithCancellationToken_ResolvesToken()
    {
        EdgeCaseLog.Reset();
        var settings = new CommandRuntimeSettings();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var (handler, parseResult, _) = CreateHandler<RunAsyncWithCancellationTokenCommand>([], settings);

        await handler.InvokeAsync(parseResult, arg2: null, cts.Token);

        EdgeCaseLog.RunAsyncCancellationCount.ShouldBe(expected: 1);
    }

    [Fact]
    public void Invoke_RunWithContext_IncludesContext()
    {
        HandlerLog.Reset();
        var settings = new CommandRuntimeSettings();
        var (handler, parseResult, _) = CreateHandler<RunWithContextCommand>(["--trigger"], settings);

        handler.Invoke(parseResult, arg2: null);

        HandlerLog.ContextCount.ShouldBe(expected: 1);
        HandlerLog.LastContext.ShouldNotBeNull();
    }

    [Fact]
    public void Invoke_RunWithServiceParameter_ResolvesDependency()
    {
        HandlerLog.Reset();
        var settings = new CommandRuntimeSettings();
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("service"));
        var provider = services.BuildServiceProvider();
        var (handler, parseResult, _) = CreateHandler<RunWithServiceCommand>(["--trigger"], settings);
        var resolver = new ServiceProviderResolver(provider);

        handler.Invoke(parseResult, resolver);

        HandlerLog.RunCount.ShouldBe(expected: 1);
        HandlerLog.LastServiceValue.ShouldBe("service");
    }

    [Fact]
    public void Invoke_RunWithContextAndService_ResolvesDependencyAndContext()
    {
        HandlerLog.Reset();
        var settings = new CommandRuntimeSettings();
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("service"));
        var provider = services.BuildServiceProvider();
        var (handler, parseResult, _) = CreateHandler<RunWithContextAndServiceCommand>(["--trigger"], settings);
        var resolver = new ServiceProviderResolver(provider);

        handler.Invoke(parseResult, resolver);

        HandlerLog.RunCount.ShouldBe(expected: 1);
        HandlerLog.ContextCount.ShouldBe(expected: 1);
        HandlerLog.LastContext.ShouldNotBeNull();
        HandlerLog.LastServiceValue.ShouldBe("service");
    }

    [Fact]
    public async Task InvokeAsync_RunAsyncWithServiceAndCancellationToken_ResolvesDependencyAndToken()
    {
        HandlerLog.Reset();
        var settings = new CommandRuntimeSettings();
        var services = new ServiceCollection();
        services.AddSingleton(new DiDependency("service"));
        var provider = services.BuildServiceProvider();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var (handler, parseResult, _) = CreateHandler<RunAsyncWithServiceAndTokenCommand>(["--trigger"], settings);
        var resolver = new ServiceProviderResolver(provider);

        await handler.InvokeAsync(parseResult, resolver, cts.Token);

        HandlerLog.RunAsyncCount.ShouldBe(expected: 1);
        HandlerLog.LastServiceValue.ShouldBe("service");
        HandlerLog.LastTokenCanceled.ShouldBeTrue();
    }

    [Fact]
    public void Invoke_RunWithRequiredServiceMissing_Throws()
    {
        HandlerLog.Reset();
        var settings = new CommandRuntimeSettings();
        var (handler, parseResult, _) = CreateHandler<RunWithRequiredServiceCommand>(["--trigger"], settings);

        Should.Throw<InvalidOperationException>(() => handler.Invoke(parseResult, arg2: null));
    }

    [Fact]
    public void Invoke_RunWithOptionalServiceMissing_UsesDefault()
    {
        HandlerLog.Reset();
        var settings = new CommandRuntimeSettings();
        var (handler, parseResult, _) = CreateHandler<RunWithOptionalServiceCommand>(["--trigger"], settings);

        handler.Invoke(parseResult, arg2: null);

        HandlerLog.RunCount.ShouldBe(expected: 1);
        HandlerLog.OptionalServiceWasNull.ShouldBeTrue();
    }

    [Fact]
    public void TryCreateHandler_ContextNotFirst_FallsBackToHelp()
    {
        EdgeCaseLog.Reset();
        var settings = new CommandRuntimeSettings();
        var (handler, parseResult, output) = CreateHandler<RunWithContextNotFirstCommand>([], settings);

        handler.Invoke(parseResult, arg2: null);

        EdgeCaseLog.RunContextNotFirstCount.ShouldBe(expected: 0);
        output.ToString().ToLowerInvariant().ShouldContain("usage");
    }

    [Fact]
    public void TryCreateHandler_CancellationTokenNotLast_FallsBackToHelp()
    {
        EdgeCaseLog.Reset();
        var settings = new CommandRuntimeSettings();
        var (handler, parseResult, output) = CreateHandler<RunAsyncWithTokenNotLastCommand>([], settings);

        handler.Invoke(parseResult, arg2: null);

        EdgeCaseLog.RunTokenNotLastCount.ShouldBe(expected: 0);
        output.ToString().ToLowerInvariant().ShouldContain("usage");
    }

    [Fact]
    public void TryCreateHandler_PrivateRun_IgnoresAndFallsBackToHelp()
    {
        EdgeCaseLog.Reset();
        var settings = new CommandRuntimeSettings();
        var (handler, parseResult, output) = CreateHandler<RunPrivateCommand>([], settings);

        handler.Invoke(parseResult, arg2: null);

        EdgeCaseLog.RunPrivateCount.ShouldBe(expected: 0);
        output.ToString().ToLowerInvariant().ShouldContain("usage");
    }

    [Fact]
    public void TryCreateHandler_RunOverload_UsesFirstShapeMethod()
    {
        EdgeCaseLog.Reset();
        var settings = new CommandRuntimeSettings();
        var (handler, parseResult, _) = CreateHandler<RunOverloadConflictCommand>([], settings);
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<RunOverloadConflictCommand>();
        var selected = shape.Methods.First(method => method.Name == "Run");

        handler.Invoke(parseResult, arg2: null);

        if (selected.Parameters.Count == 0)
        {
            EdgeCaseLog.RunOverloadCount.ShouldBe(expected: 1);
            EdgeCaseLog.RunOverloadContextCount.ShouldBe(expected: 0);
        }
        else
        {
            EdgeCaseLog.RunOverloadContextCount.ShouldBe(expected: 1);
            EdgeCaseLog.RunOverloadCount.ShouldBe(expected: 0);
        }
    }

    [Fact]
    public void Invoke_EmptyCommandWithShowHelp_SuppressesHandler()
    {
        HandlerLog.Reset();
        var settings = new CommandRuntimeSettings { ShowHelpOnEmptyCommand = true };
        var (handler, parseResult, output) = CreateHandler<RunCommand>([], settings);

        handler.Invoke(parseResult, arg2: null);

        HandlerLog.RunCount.ShouldBe(expected: 0);
        output.ToString().ToLowerInvariant().ShouldContain("usage");
    }

    [Fact]
    public void Invoke_NoHandler_ShowsHelpAndReturnsZero()
    {
        var settings = new CommandRuntimeSettings();
        var (handler, parseResult, output) = CreateHandler<NoRunCommand>([], settings);

        var code = handler.Invoke(parseResult, arg2: null);

        code.ShouldBe(expected: 0);
        output.ToString().ToLowerInvariant().ShouldContain("usage");
    }

    private static (CommandHandler Handler, ParseResult ParseResult, StringWriter Output) CreateHandler<TCommand>(
        string[] args,
        CommandRuntimeSettings settings) where TCommand : IShapeable<TCommand>
    {
        var output = new StringWriter();
        settings.Output = output;
        settings.Error = output;

        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<TCommand>();
        var definition = CommandModelFactory.BuildFromObject(shape, shape.Provider);
        var runtime = CommandRuntimeBuilder.Build(definition, settings);
        var bindingContext = runtime.BindingContext;
        var parseResult = runtime.Graph.RootCommand.Parse(args);
        var handler = CommandHandlerFactory.TryCreateHandler(shape, bindingContext, settings, convention: null);

        handler.ShouldNotBeNull();
        return (handler!, parseResult, output);
    }
}