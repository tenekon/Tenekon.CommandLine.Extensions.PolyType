namespace Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

/// <summary>
/// Provides context for visiting member or parameter specs.
/// </summary>
public sealed class NodeVisitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeVisitContext"/> class.
    /// </summary>
    /// <param name="node">The owning command node.</param>
    public NodeVisitContext(CommandModelBuilderNode node)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
    }

    /// <summary>
    /// Gets the owning command node.
    /// </summary>
    public CommandModelBuilderNode Node { get; }
}
