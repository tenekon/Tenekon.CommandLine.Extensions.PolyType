using System.CommandLine;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Graph;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime.Graph;

public class RuntimeValueAccessorTests
{
    [Fact]
    public void Getter_ReturnsValueFromInstance()
    {
        var accessor = new RuntimeValueAccessor("value", (instance, _) => ((Sample)instance!).Value);

        var result = accessor.Getter(new Sample("test"), new RootCommand().Parse([]));

        result.ShouldBe("test");
    }

    private sealed class Sample(string value)
    {
        public string Value { get; } = value;
    }
}