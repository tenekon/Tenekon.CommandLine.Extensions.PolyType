namespace Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

/// <summary>
/// Base visitor that walks a <see cref="CommandModelBuilder"/> and its nodes.
/// </summary>
public class CommandModelBuilderNodeVisitor : ICommandModelBuilderNodeVisitor
{
    private readonly HashSet<CommandModelBuilderNode> _visited = [];

    /// <summary>
    /// Visits every node in the builder.
    /// </summary>
    /// <param name="builder">The builder to visit.</param>
    public void Visit(CommandModelBuilder builder)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        _visited.Clear();

        foreach (var node in builder.Nodes)
        {
            VisitNode(node);
        }
    }

    /// <inheritdoc />
    public virtual void VisitObject(CommandObjectModelBuilderNode node)
    {
        foreach (var member in node.Members)
        {
            VisitMember(new NodeVisitContext(node), member);
        }

        VisitChildren(node);
    }

    /// <inheritdoc />
    public virtual void VisitFunction(CommandFunctionModelBuilderNode node)
    {
        foreach (var parameter in node.Parameters)
        {
            VisitParameter(new NodeVisitContext(node), parameter);
        }

        VisitChildren(node);
    }

    /// <inheritdoc />
    public virtual void VisitMethod(CommandMethodModelBuilderNode node)
    {
        foreach (var parameter in node.Parameters)
        {
            VisitParameter(new NodeVisitContext(node), parameter);
        }

        VisitChildren(node);
    }

    /// <inheritdoc />
    public virtual void VisitMember(NodeVisitContext context, CommandMemberSpecBuilder member)
    {
        if (member.Option is not null)
            VisitOption(new SpecVisitContext(context.Node, member, parameter: null), member.Option);
        if (member.Argument is not null)
            VisitArgument(new SpecVisitContext(context.Node, member, parameter: null), member.Argument);
        if (member.Directive is not null)
            VisitDirective(new SpecVisitContext(context.Node, member, parameter: null), member.Directive);
    }

    /// <inheritdoc />
    public virtual void VisitParameter(NodeVisitContext context, CommandParameterSpecBuilder parameter)
    {
        if (parameter.Option is not null)
            VisitOption(new SpecVisitContext(context.Node, member: null, parameter), parameter.Option);
        if (parameter.Argument is not null)
            VisitArgument(new SpecVisitContext(context.Node, member: null, parameter), parameter.Argument);
        if (parameter.Directive is not null)
            VisitDirective(new SpecVisitContext(context.Node, member: null, parameter), parameter.Directive);
    }

    /// <inheritdoc />
    public virtual void VisitOption(SpecVisitContext context, OptionSpecBuilder option)
    {
    }

    /// <inheritdoc />
    public virtual void VisitArgument(SpecVisitContext context, ArgumentSpecBuilder argument)
    {
    }

    /// <inheritdoc />
    public virtual void VisitDirective(SpecVisitContext context, DirectiveSpecBuilder directive)
    {
    }

    /// <summary>
    /// Visits the children of an object node.
    /// </summary>
    /// <param name="node">The node whose children are visited.</param>
    protected void VisitChildren(CommandObjectModelBuilderNode node)
    {
        foreach (var child in node.Children)
        {
            VisitNode(child);
        }

        foreach (var method in node.MethodChildren)
        {
            VisitNode(method);
        }
    }

    /// <summary>
    /// Visits the children of a function node.
    /// </summary>
    /// <param name="node">The node whose children are visited.</param>
    protected void VisitChildren(CommandFunctionModelBuilderNode node)
    {
        foreach (var child in node.Children)
        {
            VisitNode(child);
        }
    }

    /// <summary>
    /// Visits the children of a method node.
    /// </summary>
    /// <param name="node">The node whose children are visited.</param>
    protected void VisitChildren(CommandMethodModelBuilderNode node)
    {
        foreach (var child in node.Children)
        {
            VisitNode(child);
        }
    }

    private void VisitNode(CommandModelBuilderNode node)
    {
        if (!_visited.Add(node)) return;
        node.Accept(this);
    }
}
