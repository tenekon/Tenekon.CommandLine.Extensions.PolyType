using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime;

public class ParentInjectionTests
{
    [Fact]
    public void Run_ChildCommand_InjectsParentAndContextInConstructor()
    {
        ParentInjectionLog.Reset();
        var fixture = new CommandRuntimeFixture();
        var app = fixture.CreateApp<ParentInjectionRootCommand>();

        var result = app.Parse(["child-run"]);

        result.Run();

        var parentInstance = result.Bind<ParentInjectionRootCommand>();
        var childInstance = result.Bind<ParentInjectionChildRunCommand>();

        childInstance.ParentFromConstructor.ShouldBe(parentInstance);
        ParentInjectionLog.ConstructorParent.ShouldBe(parentInstance);
        ParentInjectionLog.ConstructorContextSeen.ShouldBeTrue();
        ParentInjectionLog.RunParent.ShouldBe(parentInstance);
    }

    [Fact]
    public async Task RunAsync_InjectsParentAndCancellationToken()
    {
        ParentInjectionLog.Reset();
        var fixture = new CommandRuntimeFixture();
        var app = fixture.CreateApp<ParentInjectionRootCommand>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = app.Parse(["child-run-async"]);

#pragma warning disable xUnit1051
        await result.RunAsync(cts.Token);
#pragma warning restore xUnit1051

        var parentInstance = result.Bind<ParentInjectionRootCommand>();

        ParentInjectionLog.RunAsyncConstructorParent.ShouldBe(parentInstance);
        ParentInjectionLog.RunAsyncConstructorTokenCanceled.ShouldBeTrue();
        ParentInjectionLog.RunAsyncParent.ShouldBe(parentInstance);
        ParentInjectionLog.RunAsyncTokenCanceled.ShouldBeTrue();
    }

    [Fact]
    public void Run_ParentInjection_TakesPrecedenceOverServiceResolution()
    {
        ParentInjectionLog.Reset();
        var services = new ServiceCollection();
        var fakeParent = new ParentInjectionRootCommand();
        services.AddSingleton(fakeParent);
        var provider = services.BuildServiceProvider();
        var fixture = new CommandRuntimeFixture();
        var app = fixture.CreateApp<ParentInjectionRootCommand>(provider);

        var result = app.Parse(["child-run"]);

        result.Run();

        var parentInstance = result.Bind<ParentInjectionRootCommand>();

        ParentInjectionLog.RunParent.ShouldBe(parentInstance);
        ParentInjectionLog.RunParent.ShouldNotBe(fakeParent);
    }

    [Fact]
    public void Run_FunctionCommand_InjectsParent()
    {
        ParentInjectionLog.Reset();
        var fixture = new CommandRuntimeFixture();
        var app = fixture.CreateApp<ParentInjectionRootCommand>();
        app.FunctionRegistry.Set<ParentInjectionFunctionCommand>(parent =>
        {
            ParentInjectionLog.FunctionParent = parent;
        });

        var result = app.Parse(["function-child"]);

        result.Run();

        var parentInstance = result.Bind<ParentInjectionRootCommand>();
        ParentInjectionLog.FunctionParent.ShouldBe(parentInstance);
    }
}