using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal sealed class CommandFunctionNode(
    Type functionType,
    IFunctionTypeShape functionShape,
    CommandSpecAttribute spec,
    IReadOnlyList<ParameterSpecEntry> parameterSpecs) : ICommandGraphNode
{
    public Type FunctionType { get; } = functionType;
    public IFunctionTypeShape FunctionShape { get; } = functionShape;
    public CommandSpecAttribute Spec { get; } = spec;
    public IReadOnlyList<ParameterSpecEntry> ParameterSpecs { get; } = parameterSpecs;
    public ICommandGraphNode? Parent { get; set; }
    public List<ICommandGraphNode> Children { get; } = [];
    public string DisplayName => FunctionType.Name;
    public Type? CommandType => FunctionType;
}