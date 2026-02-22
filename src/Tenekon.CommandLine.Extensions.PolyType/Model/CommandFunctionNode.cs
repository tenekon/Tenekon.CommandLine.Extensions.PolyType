using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal sealed class CommandFunctionNode(
    Type functionType,
    IFunctionTypeShape functionShape,
    CommandSpecModel spec,
    IReadOnlyList<ParameterSpecEntry> parameterSpecs) : ICommandGraphNode
{
    public Type FunctionType { get; } = functionType;
    public IFunctionTypeShape FunctionShape { get; } = functionShape;
    public CommandSpecModel Spec { get; } = spec;
    public IReadOnlyList<ParameterSpecEntry> ParameterSpecs { get; } = parameterSpecs;
    public ICommandGraphNode? Parent { get; internal set; }
    public List<ICommandGraphNode> Children { get; } = [];
    public string DisplayName => FunctionType.Name;
    public Type? CommandType => FunctionType;

    IReadOnlyList<ICommandGraphNode> ICommandGraphNode.Children => Children;
}