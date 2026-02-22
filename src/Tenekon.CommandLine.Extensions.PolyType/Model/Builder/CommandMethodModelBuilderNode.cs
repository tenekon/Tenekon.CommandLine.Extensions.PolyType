using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

/// <summary>
/// Builder node for method-based child commands.
/// </summary>
public sealed class CommandMethodModelBuilderNode : CommandModelBuilderNode
{
    internal CommandMethodModelBuilderNode(
        CommandObjectModelBuilderNode parentType,
        IMethodShape methodShape,
        CommandSpecModel specModel) : base(specModel)
    {
        ParentType = parentType;
        MethodShape = methodShape;
        Parent = parentType;
    }

    /// <summary>
    /// Gets the parent object command node that declares this method.
    /// </summary>
    public CommandObjectModelBuilderNode ParentType { get; }

    /// <summary>
    /// Gets the PolyType method shape for this command.
    /// </summary>
    public IMethodShape MethodShape { get; }

    /// <summary>
    /// Gets the parameter spec builders for this method.
    /// </summary>
    public IList<CommandParameterSpecBuilder> Parameters { get; } = new List<CommandParameterSpecBuilder>();

    /// <inheritdoc />
    public override string DisplayName => MethodShape.Name;

    /// <inheritdoc />
    public override Type? CommandType => null;

    /// <inheritdoc />
    public override void Accept(ICommandModelBuilderNodeVisitor visitor)
    {
        if (visitor is null) throw new ArgumentNullException(nameof(visitor));
        visitor.VisitMethod(this);
    }

    /// <summary>
    /// Sets an option spec for the specified parameter.
    /// </summary>
    /// <param name="parameter">The parameter to configure.</param>
    /// <param name="spec">The option spec model.</param>
    /// <returns>The created parameter spec builder.</returns>
    public CommandParameterSpecBuilder SetOption(IParameterShape parameter, OptionSpecModel spec)
    {
        return SetParameterSpec(parameter, spec, argument: null, directive: null);
    }

    /// <summary>
    /// Sets an argument spec for the specified parameter.
    /// </summary>
    /// <param name="parameter">The parameter to configure.</param>
    /// <param name="spec">The argument spec model.</param>
    /// <returns>The created parameter spec builder.</returns>
    public CommandParameterSpecBuilder SetArgument(IParameterShape parameter, ArgumentSpecModel spec)
    {
        return SetParameterSpec(parameter, option: null, spec, directive: null);
    }

    /// <summary>
    /// Sets a directive spec for the specified parameter.
    /// </summary>
    /// <param name="parameter">The parameter to configure.</param>
    /// <param name="spec">The directive spec model.</param>
    /// <returns>The created parameter spec builder.</returns>
    public CommandParameterSpecBuilder SetDirective(IParameterShape parameter, DirectiveSpecModel spec)
    {
        return SetParameterSpec(parameter, option: null, argument: null, spec);
    }

    private CommandParameterSpecBuilder SetParameterSpec(
        IParameterShape parameter,
        OptionSpecModel? option,
        ArgumentSpecModel? argument,
        DirectiveSpecModel? directive)
    {
        ClearSpec(parameter);
        var entry = new CommandParameterSpecBuilder(
            parameter,
            option is null ? null : new OptionSpecBuilder(option),
            argument is null ? null : new ArgumentSpecBuilder(argument),
            directive is null ? null : new DirectiveSpecBuilder(directive));
        Parameters.Add(entry);
        return entry;
    }

    /// <summary>
    /// Clears any CLI spec for the specified parameter.
    /// </summary>
    /// <param name="parameter">The parameter to clear.</param>
    /// <returns><see langword="true" /> if the spec was removed; otherwise <see langword="false" />.</returns>
    public bool ClearSpec(IParameterShape parameter)
    {
        var index = Parameters.ToList().FindIndex(entry => ReferenceEquals(entry.Parameter, parameter));
        if (index < 0) return false;
        Parameters.RemoveAt(index);
        return true;
    }
}
