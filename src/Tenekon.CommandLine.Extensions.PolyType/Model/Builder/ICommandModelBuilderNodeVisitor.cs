namespace Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

/// <summary>
/// Defines a visitor for command model builder nodes and their specs.
/// </summary>
public interface ICommandModelBuilderNodeVisitor
{
    /// <summary>
    /// Visits a command member spec builder.
    /// </summary>
    /// <param name="context">Context describing the owning node.</param>
    /// <param name="member">The member spec builder.</param>
    void VisitMember(NodeVisitContext context, CommandMemberSpecBuilder member);

    /// <summary>
    /// Visits a command parameter spec builder.
    /// </summary>
    /// <param name="context">Context describing the owning node.</param>
    /// <param name="parameter">The parameter spec builder.</param>
    void VisitParameter(NodeVisitContext context, CommandParameterSpecBuilder parameter);

    /// <summary>
    /// Visits an option spec builder.
    /// </summary>
    /// <param name="context">Context describing the owning member or parameter.</param>
    /// <param name="option">The option spec builder.</param>
    void VisitOption(SpecVisitContext context, OptionSpecBuilder option);

    /// <summary>
    /// Visits an argument spec builder.
    /// </summary>
    /// <param name="context">Context describing the owning member or parameter.</param>
    /// <param name="argument">The argument spec builder.</param>
    void VisitArgument(SpecVisitContext context, ArgumentSpecBuilder argument);

    /// <summary>
    /// Visits a directive spec builder.
    /// </summary>
    /// <param name="context">Context describing the owning member or parameter.</param>
    /// <param name="directive">The directive spec builder.</param>
    void VisitDirective(SpecVisitContext context, DirectiveSpecBuilder directive);

    /// <summary>
    /// Visits an object command node.
    /// </summary>
    /// <param name="node">The node to visit.</param>
    void VisitObject(CommandObjectModelBuilderNode node);

    /// <summary>
    /// Visits a function command node.
    /// </summary>
    /// <param name="node">The node to visit.</param>
    void VisitFunction(CommandFunctionModelBuilderNode node);

    /// <summary>
    /// Visits a method command node.
    /// </summary>
    /// <param name="node">The node to visit.</param>
    void VisitMethod(CommandMethodModelBuilderNode node);
}
