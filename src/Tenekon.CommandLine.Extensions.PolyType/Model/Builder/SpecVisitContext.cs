using System.Diagnostics.CodeAnalysis;

namespace Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

/// <summary>
/// Provides context for visiting a specific CLI spec.
/// </summary>
public sealed class SpecVisitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpecVisitContext"/> class.
    /// </summary>
    /// <param name="node">The owning command node.</param>
    /// <param name="member">The owning member spec builder, if any.</param>
    /// <param name="parameter">The owning parameter spec builder, if any.</param>
    public SpecVisitContext(
        CommandModelBuilderNode node,
        CommandMemberSpecBuilder? member,
        CommandParameterSpecBuilder? parameter)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        Member = member;
        Parameter = parameter;
    }

    /// <summary>
    /// Gets the owning command node.
    /// </summary>
    public CommandModelBuilderNode Node { get; }

    /// <summary>
    /// Gets the owning member spec builder, if any.
    /// </summary>
    public CommandMemberSpecBuilder? Member { get; }

    /// <summary>
    /// Gets the owning parameter spec builder, if any.
    /// </summary>
    public CommandParameterSpecBuilder? Parameter { get; }

    /// <summary>
    /// Gets a value indicating whether this spec is associated with a member.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Member))]
    public bool IsMember => Member is not null;

    /// <summary>
    /// Gets a value indicating whether this spec is associated with a parameter.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Parameter))]
    public bool IsParameter => Parameter is not null;
}
