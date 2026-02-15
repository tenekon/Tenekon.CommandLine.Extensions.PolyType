using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal sealed class CommandMethodNode(
    CommandModelNode parentType,
    IMethodShape methodShape,
    CommandSpecAttribute spec,
    IReadOnlyList<ParameterSpecEntry> parameterSpecs) : ICommandGraphNode
{
    public CommandModelNode ParentType { get; } = parentType;
    public IMethodShape MethodShape { get; } = methodShape;
    public CommandSpecAttribute Spec { get; } = spec;
    public IReadOnlyList<ParameterSpecEntry> ParameterSpecs { get; } = parameterSpecs;
    public ICommandGraphNode? Parent { get; set; } = parentType;
    public List<ICommandGraphNode> Children { get; } = [];
    public string DisplayName => MethodShape.Name;
    public Type? CommandType => null;
}