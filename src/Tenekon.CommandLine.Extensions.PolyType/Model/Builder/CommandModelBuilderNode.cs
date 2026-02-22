namespace Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

/// <summary>
/// Base type for builder nodes that compose a command model graph.
/// </summary>
public abstract class CommandModelBuilderNode
{
    private readonly List<CommandModelBuilderNode> _children = [];

    /// <summary>
    /// Initializes a new builder node with the given specification model.
    /// </summary>
    /// <param name="specModel">Specification model used to seed the builder.</param>
    protected CommandModelBuilderNode(CommandSpecModel specModel)
    {
        Spec = new CommandSpecBuilder(specModel);
    }

    /// <summary>Gets the command specification builder for this node.</summary>
    public CommandSpecBuilder Spec { get; }
    /// <summary>Gets the parent node, if any.</summary>
    public CommandModelBuilderNode? Parent { get; internal set; }
    /// <summary>Gets the child nodes.</summary>
    public IReadOnlyList<CommandModelBuilderNode> Children => _children;
    internal List<CommandModelBuilderNode> ChildrenMutable => _children;
    /// <summary>Gets the display name for diagnostics and help.</summary>
    public abstract string DisplayName { get; }
    /// <summary>Gets the command type represented by this node, if any.</summary>
    public abstract Type? CommandType { get; }
    
    /// <summary>
    /// Accepts a visitor for this node.
    /// </summary>
    /// <param name="visitor">Visitor instance.</param>
    public abstract void Accept(ICommandModelBuilderNodeVisitor visitor);
}
