using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

/// <summary>
/// Builder node for object-based commands.
/// </summary>
public sealed class CommandObjectModelBuilderNode : CommandModelBuilderNode
{
    internal CommandObjectModelBuilderNode(
        Type definitionType,
        IObjectTypeShape shape,
        CommandSpecModel specModel,
        CommandHandlerConventionSpecModel handlerConvention) : base(specModel)
    {
        DefinitionType = definitionType;
        Shape = shape;
        HandlerConvention = new CommandHandlerConventionSpecBuilder(handlerConvention);
    }

    internal List<CommandMethodModelBuilderNode> MethodChildrenMutable { get; } = [];

    /// <summary>
    /// Gets the concrete command type.
    /// </summary>
    public Type DefinitionType { get; }

    /// <summary>
    /// Gets the PolyType shape for the command type.
    /// </summary>
    public IObjectTypeShape Shape { get; }

    /// <summary>
    /// Gets the member spec builders for this command.
    /// </summary>
    public IList<CommandMemberSpecBuilder> Members { get; } = new List<CommandMemberSpecBuilder>();

    /// <summary>
    /// Gets the handler convention builder for this command.
    /// </summary>
    public CommandHandlerConventionSpecBuilder HandlerConvention { get; }

    /// <summary>
    /// Gets the method command children declared on this type.
    /// </summary>
    public IReadOnlyList<CommandMethodModelBuilderNode> MethodChildren => MethodChildrenMutable;

    /// <summary>
    /// Adds or configures an option spec for a property.
    /// </summary>
    /// <param name="property">The property that defines the spec.</param>
    /// <param name="spec">The option spec model.</param>
    /// <param name="targetProperty">Optional target property for binding.</param>
    /// <param name="ownerType">Optional owner type for the spec property.</param>
    /// <returns>The created member spec builder.</returns>
    public CommandMemberSpecBuilder AddOption(
        IPropertyShape property,
        OptionSpecModel spec,
        IPropertyShape? targetProperty = null,
        Type? ownerType = null)
    {
        var entry = new CommandMemberSpecBuilder(
            ownerType ?? DefinitionType,
            property,
            targetProperty ?? property,
            new OptionSpecBuilder(spec),
            argument: null,
            directive: null);

        Members.Add(entry);
        return entry;
    }

    /// <summary>
    /// Adds or configures an argument spec for a property.
    /// </summary>
    /// <param name="property">The property that defines the spec.</param>
    /// <param name="spec">The argument spec model.</param>
    /// <param name="targetProperty">Optional target property for binding.</param>
    /// <param name="ownerType">Optional owner type for the spec property.</param>
    /// <returns>The created member spec builder.</returns>
    public CommandMemberSpecBuilder AddArgument(
        IPropertyShape property,
        ArgumentSpecModel spec,
        IPropertyShape? targetProperty = null,
        Type? ownerType = null)
    {
        var entry = new CommandMemberSpecBuilder(
            ownerType ?? DefinitionType,
            property,
            targetProperty ?? property,
            option: null,
            new ArgumentSpecBuilder(spec),
            directive: null);

        Members.Add(entry);
        return entry;
    }

    /// <summary>
    /// Adds or configures a directive spec for a property.
    /// </summary>
    /// <param name="property">The property that defines the spec.</param>
    /// <param name="spec">The directive spec model.</param>
    /// <param name="targetProperty">Optional target property for binding.</param>
    /// <param name="ownerType">Optional owner type for the spec property.</param>
    /// <returns>The created member spec builder.</returns>
    public CommandMemberSpecBuilder AddDirective(
        IPropertyShape property,
        DirectiveSpecModel spec,
        IPropertyShape? targetProperty = null,
        Type? ownerType = null)
    {
        var entry = new CommandMemberSpecBuilder(
            ownerType ?? DefinitionType,
            property,
            targetProperty ?? property,
            option: null,
            argument: null,
            new DirectiveSpecBuilder(spec));

        Members.Add(entry);
        return entry;
    }

    /// <summary>
    /// Removes a member spec by property shape.
    /// </summary>
    /// <param name="property">The property defining the spec.</param>
    /// <returns><see langword="true" /> if the member was removed; otherwise <see langword="false" />.</returns>
    public bool RemoveMember(IPropertyShape property)
    {
        var index = Members.ToList().FindIndex(entry => ReferenceEquals(entry.SpecProperty, property));
        if (index < 0) return false;
        Members.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Replaces a member spec entry for the given property.
    /// </summary>
    /// <param name="property">The property defining the spec.</param>
    /// <param name="option">The option spec, if any.</param>
    /// <param name="argument">The argument spec, if any.</param>
    /// <param name="directive">The directive spec, if any.</param>
    /// <param name="targetProperty">Optional target property for binding.</param>
    /// <param name="ownerType">Optional owner type for the spec property.</param>
    /// <returns>The created member spec builder.</returns>
    public CommandMemberSpecBuilder ReplaceMember(
        IPropertyShape property,
        OptionSpecModel? option,
        ArgumentSpecModel? argument,
        DirectiveSpecModel? directive,
        IPropertyShape? targetProperty = null,
        Type? ownerType = null)
    {
        RemoveMember(property);
        var entry = new CommandMemberSpecBuilder(
            ownerType ?? DefinitionType,
            property,
            targetProperty ?? property,
            option is null ? null : new OptionSpecBuilder(option),
            argument is null ? null : new ArgumentSpecBuilder(argument),
            directive is null ? null : new DirectiveSpecBuilder(directive));
        Members.Add(entry);
        return entry;
    }

    /// <inheritdoc />
    public override string DisplayName => DefinitionType.Name;

    /// <inheritdoc />
    public override Type? CommandType => DefinitionType;

    /// <inheritdoc />
    public override void Accept(ICommandModelBuilderNodeVisitor visitor)
    {
        if (visitor is null) throw new ArgumentNullException(nameof(visitor));
        visitor.VisitObject(this);
    }
}
