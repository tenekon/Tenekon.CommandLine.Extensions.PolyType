using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal interface ICommandGraphNode
{
    CommandSpecAttribute Spec { get; }
    ICommandGraphNode? Parent { get; set; }
    List<ICommandGraphNode> Children { get; }
    string DisplayName { get; }
    Type? CommandType { get; }
}