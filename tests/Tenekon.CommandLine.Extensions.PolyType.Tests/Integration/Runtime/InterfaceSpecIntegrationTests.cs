using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime;

public class InterfaceSpecIntegrationTests
{
    [Fact]
    public void Bind_InterfaceSpecOption_BindsInterfaceInstance()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<InterfaceSpecCommand>(["--iface-option", "value", "argument"]);
        var target = new InterfaceSpecOptionTarget();

        result.Bind<InterfaceSpecCommand, IInterfaceSpecOption>(target);

        target.OptionValue.ShouldBe("value");
    }

    [Fact]
    public void Bind_InterfaceSpecArgument_BindsInterfaceInstance()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<InterfaceSpecCommand>(["--iface-option", "value", "argument"]);
        var target = new InterfaceSpecArgumentTarget();

        result.Bind<InterfaceSpecCommand, IInterfaceSpecArgument>(target);

        target.ArgumentValue.ShouldBe("argument");
    }

    [Fact]
    public void Bind_ClassTarget_IncludesInterfaceSpecs()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<InterfaceSpecCommand>(["--iface-option", "value", "argument"]);
        var target = new InterfaceSpecCommand();

        result.Bind<InterfaceSpecCommand, InterfaceSpecCommand>(target);

        target.OptionValue.ShouldBe("value");
        target.ArgumentValue.ShouldBe("argument");
    }

    [Fact]
    public void Bind_ExplicitInterfaceProperty_BindsValue()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<InterfaceSpecExplicitCommand>(["--explicit", "value"]);
        var target = new InterfaceSpecExplicitCommand();

        result.Bind<InterfaceSpecExplicitCommand, IInterfaceSpecExplicit>(target);

        ((IInterfaceSpecExplicit)target).ExplicitValue.ShouldBe("value");
    }

    [Fact]
    public void Bind_InterfaceDefaultValue_UsesInstanceDefault()
    {
        var fixture = new CommandRuntimeFixture();
        var result = fixture.Parse<InterfaceSpecDefaultCommand>([]);
        var target = new InterfaceSpecDefaultCommand();

        result.Bind<InterfaceSpecDefaultCommand, IInterfaceSpecDefault>(target);

        target.DefaultValue.ShouldBe("from-default");
    }
}