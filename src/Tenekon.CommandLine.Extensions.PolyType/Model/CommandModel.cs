using Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

/// <summary>
/// Represents an immutable command model graph.
/// </summary>
public sealed class CommandModel
{
    internal CommandModel(CommandModelGraph graph)
    {
        Graph = graph;
    }

    internal CommandModelGraph Graph { get; }

    /// <summary>Gets the root node of the command graph.</summary>
    public ICommandGraphNode Root => Graph.RootNode;

    /// <summary>
    /// Creates a mutable builder initialized from this model.
    /// </summary>
    public CommandModelBuilder ToBuilder()
    {
        return new CommandModelBuilder(this);
    }

    /// <summary>
    /// Gets the root command type of the model.
    /// </summary>
    public Type DefinitionType =>
        Graph.RootNode.CommandType ?? throw new InvalidOperationException("Root command type is not available.");
}
