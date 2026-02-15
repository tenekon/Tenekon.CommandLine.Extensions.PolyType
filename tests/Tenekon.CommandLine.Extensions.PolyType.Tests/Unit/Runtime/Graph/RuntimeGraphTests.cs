using System.CommandLine;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Graph;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime.Graph;

public class RuntimeGraphTests
{
    [Fact]
    public void RuntimeGraph_StoresRootCommandAndNode()
    {
        var rootCommand = new RootCommand();
        var rootNode = RuntimeNode.CreateType(typeof(string), rootCommand, [], []);

        var graph = new RuntimeGraph(rootCommand, rootNode);

        graph.RootCommand.ShouldBe(rootCommand);
        graph.RootNode.ShouldBe(rootNode);
    }
}