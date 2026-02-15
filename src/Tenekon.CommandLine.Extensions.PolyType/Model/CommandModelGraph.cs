namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal sealed class CommandModelGraph(
    ICommandGraphNode rootNode,
    IReadOnlyList<CommandMethodNode> methodNodes,
    IReadOnlyList<CommandFunctionNode> functionNodes)
{
    public ICommandGraphNode RootNode { get; } = rootNode;
    public IReadOnlyList<CommandMethodNode> MethodNodes { get; } = methodNodes;
    public IReadOnlyList<CommandFunctionNode> FunctionNodes { get; } = functionNodes;
}