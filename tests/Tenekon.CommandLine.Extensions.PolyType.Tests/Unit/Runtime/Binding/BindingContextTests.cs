using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime.Binding;

public class BindingContextTests
{
    [Fact]
    public void Bind_ParseResultCache_ReturnsSameInstance()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<RootWithChildrenCommand>(["child-a"]);

        var first = result.Bind<RootWithChildrenCommand>();
        var second = result.Bind<RootWithChildrenCommand>();

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void Bind_ReturnEmpty_BypassesCache()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<RootWithChildrenCommand>([]);

        var first = result.Bind<RootWithChildrenCommand>(returnEmpty: true);
        var second = result.Bind<RootWithChildrenCommand>(returnEmpty: true);

        first.ShouldNotBeSameAs(second);
    }

    [Fact]
    public void Bind_ChildCommand_SetsParentAccessor()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<RootWithChildrenCommand>(["child-a"]);

        var child = (RootWithChildrenCommand.ChildACommand)result.Bind(typeof(RootWithChildrenCommand.ChildACommand));
        var root = result.Bind<RootWithChildrenCommand>();

        child.Root.ShouldBeSameAs(root);
    }

    [Fact]
    public void BindCalled_CommandInvoked_ReturnsInstance()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<RootWithChildrenCommand>(["child-b"]);

        var called = result.BindCalled();

        called.ShouldBeOfType<RootWithChildrenCommand.ChildBCommand>();
    }

    [Fact]
    public void BindAll_CalledHierarchy_ReturnsOnlyCalledInstances()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<RootWithChildrenCommand>(["child-a"]);

        var all = result.BindAll();

        all.Length.ShouldBe(expected: 2);
        all.ShouldContain(item => item is RootWithChildrenCommand);
        all.ShouldContain(item => item is RootWithChildrenCommand.ChildACommand);
        all.ShouldNotContain(item => item is RootWithChildrenCommand.ChildBCommand);
    }

    [Fact]
    public void Contains_BindCacheState_UpdatesAfterBind()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<RootWithChildrenCommand>(["child-a"]);

        result.IsCalled(typeof(RootWithChildrenCommand.ChildACommand)).ShouldBeTrue();
        result.Contains<RootWithChildrenCommand>().ShouldBeFalse();

        _ = result.Bind<RootWithChildrenCommand>();

        result.Contains<RootWithChildrenCommand>().ShouldBeTrue();
    }
}