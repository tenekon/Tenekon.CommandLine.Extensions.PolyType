namespace Tenekon.CommandLine.Extensions.PolyType.Model;

/// <summary>
/// Represents a node in the command model graph.
/// </summary>
public interface ICommandGraphNode
{
    /// <summary>Gets the command specification for this node.</summary>
    CommandSpecModel Spec { get; }
    /// <summary>Gets the parent node, if any.</summary>
    ICommandGraphNode? Parent { get; }
    /// <summary>Gets the child nodes.</summary>
    IReadOnlyList<ICommandGraphNode> Children { get; }
    /// <summary>Gets the display name for diagnostics and help.</summary>
    string DisplayName { get; }
    /// <summary>Gets the command type represented by this node, if available.</summary>
    Type? CommandType { get; }
}
