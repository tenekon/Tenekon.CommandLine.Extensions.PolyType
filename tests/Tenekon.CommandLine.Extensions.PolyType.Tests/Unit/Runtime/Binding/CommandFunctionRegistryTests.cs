using Shouldly;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime.Binding;

public class CommandFunctionRegistryTests
{
    [Fact]
    public void TryGet_WhenMissing_ReturnsFalse()
    {
        var registry = new CommandFunctionRegistry();

        registry.TryGet<SampleFunction>(out var value).ShouldBeFalse();
        value.ShouldBeNull();
    }

    [Fact]
    public void Set_OverridesExisting()
    {
        var registry = new CommandFunctionRegistry();
        SampleFunction first = () => "first";
        SampleFunction second = () => "second";

        registry.Set(first);
        registry.Set(second);

        registry.TryGet<SampleFunction>(out var value).ShouldBeTrue();
        value!.Invoke().ShouldBe("second");
    }

    [Fact]
    public void GetOrCreate_ReturnsSameInstance()
    {
        var registry = new CommandFunctionRegistry();
        var count = 0;

        var first = registry.GetOrAdd<SampleFunction>(() =>
        {
            count++;
            return () => "value";
        });

        var second = registry.GetOrAdd<SampleFunction>(() => () => "other");

        count.ShouldBe(expected: 1);
        first.ShouldBeSameAs(second);
    }

    private delegate string SampleFunction();
}