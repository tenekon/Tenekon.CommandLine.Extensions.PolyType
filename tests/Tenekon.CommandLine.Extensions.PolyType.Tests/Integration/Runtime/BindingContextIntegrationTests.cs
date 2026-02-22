using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime;

public class BindingContextIntegrationTests
{
    [Fact]
    public void TryGetBinder_CommandTarget_BindsInterfaceSpecs()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<InterfaceSpecCommand>(["--iface-option", "value", "argument"]);

        result.TryGetBinder(typeof(InterfaceSpecCommand), typeof(InterfaceSpecCommand), out var binder).ShouldBeTrue();

        binder.ShouldNotBeNull();

        var target = new InterfaceSpecCommand();
        binder(target, result.ParseResult);

        target.OptionValue.ShouldBe("value");
        target.ArgumentValue.ShouldBe("argument");
    }

    [Fact]
    public void TryGetBinder_InterfaceTarget_BindsInterfaceSpecs()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<InterfaceSpecCommand>(["--iface-option", "value", "argument"]);

        result.TryGetBinder(typeof(InterfaceSpecCommand), typeof(IInterfaceSpecOption), out var binder).ShouldBeTrue();

        binder.ShouldNotBeNull();

        var target = new InterfaceSpecOptionTarget();
        binder(target, result.ParseResult);

        target.OptionValue.ShouldBe("value");
    }

    [Fact]
    public void TryGetBinder_BaseTarget_BindsBaseSpecs()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<InterfaceSpecDerivedCommand>(["--base-opt", "value"]);

        result.TryGetBinder(typeof(InterfaceSpecDerivedCommand), typeof(InterfaceSpecBaseCommand), out var binder)
            .ShouldBeTrue();

        binder.ShouldNotBeNull();

        var target = new InterfaceSpecBaseCommand();
        binder(target, result.ParseResult);

        target.BaseValue.ShouldBe("value");
    }

    [Fact]
    public void TryGetBinder_BaseTarget_BindsInterfaceSpecsFromBase()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<InterfaceSpecDerivedFromBaseOnlyCommand>(["--base-iface-opt", "value"]);

        result.TryGetBinder(
                typeof(InterfaceSpecDerivedFromBaseOnlyCommand),
                typeof(InterfaceSpecBaseOnlyCommand),
                out var binder)
            .ShouldBeTrue();

        binder.ShouldNotBeNull();

        var target = new InterfaceSpecBaseOnlyCommand();
        binder!(target, result.ParseResult);

        target.Value.ShouldBe("value");
    }

    [Fact]
    public void TryGetBinder_BaseTarget_BindsInheritedInterfaceSpecs()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<InterfaceSpecChainDerivedCommand>(
            ["--chain-base-opt", "base", "--chain-derived-opt", "derived"]);

        result.TryGetBinder(
                typeof(InterfaceSpecChainDerivedCommand),
                typeof(InterfaceSpecChainBaseCommand),
                out var binder)
            .ShouldBeTrue();

        binder.ShouldNotBeNull();

        var target = new InterfaceSpecChainBaseCommand();
        binder!(target, result.ParseResult);

        target.BaseValue.ShouldBe("base");
        target.DerivedValue.ShouldBe("derived");
    }
}