using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal sealed class CommandMethodNode(
    CommandObjectNode parentType,
    IMethodShape methodShape,
    CommandSpecModel spec,
    IReadOnlyList<ParameterSpecEntry> parameterSpecs) : ICommandGraphNode
{
    public CommandObjectNode ParentType { get; } = parentType;
    public IMethodShape MethodShape { get; } = methodShape;
    public CommandSpecModel Spec { get; } = spec;
    public IReadOnlyList<ParameterSpecEntry> ParameterSpecs { get; } = parameterSpecs;
    public ICommandGraphNode? Parent { get; internal set; } = parentType;
    public List<ICommandGraphNode> Children { get; } = [];
    public string DisplayName => MethodShape.Name;
    public Type? CommandType => null;

    IReadOnlyList<ICommandGraphNode> ICommandGraphNode.Children => Children;
}