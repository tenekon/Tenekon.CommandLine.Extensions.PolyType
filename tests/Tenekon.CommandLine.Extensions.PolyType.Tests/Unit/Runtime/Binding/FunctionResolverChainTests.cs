using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime.Binding;

public class FunctionResolverChainTests
{
    [Fact]
    public void Create_NoResolvers_ReturnsNull()
    {
        var chain = FunctionResolverChain.Create([]);

        chain.ShouldBeNull();
    }

    [Fact]
    public void Create_SingleResolver_ReturnsInstance()
    {
        var registry = new CommandFunctionRegistry();

        var chain = FunctionResolverChain.Create([registry]);

        chain.ShouldBeSameAs(registry);
    }

    [Fact]
    public void TryResolve_UsesFirstResolver()
    {
        var first = new FixedFunctionResolver<SampleFunction>(new SampleFunction(() => "first"));
        var second = new FixedFunctionResolver<SampleFunction>(new SampleFunction(() => "second"));
        var chain = FunctionResolverChain.Create([first, second])!;

        chain.TryResolve<SampleFunction>(out var value).ShouldBeTrue();
        ((SampleFunction)value!).Invoke().ShouldBe("first");
    }

    private delegate string SampleFunction();
}