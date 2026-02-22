using System.Diagnostics.CodeAnalysis;
using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

/// <summary>
/// Describes CLI specs for a command parameter.
/// </summary>
public sealed class CommandParameterSpecBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandParameterSpecBuilder"/> class.
    /// </summary>
    /// <param name="parameter">The parameter shape.</param>
    /// <param name="option">The option spec builder, if any.</param>
    /// <param name="argument">The argument spec builder, if any.</param>
    /// <param name="directive">The directive spec builder, if any.</param>
    public CommandParameterSpecBuilder(
        IParameterShape parameter,
        OptionSpecBuilder? option,
        ArgumentSpecBuilder? argument,
        DirectiveSpecBuilder? directive)
    {
        Parameter = parameter;
        Option = option;
        Argument = argument;
        Directive = directive;
    }

    /// <summary>
    /// Gets the parameter shape.
    /// </summary>
    public IParameterShape Parameter { get; }

    /// <summary>
    /// Gets or sets the option spec builder.
    /// </summary>
    public OptionSpecBuilder? Option { get; set; }

    /// <summary>
    /// Gets or sets the argument spec builder.
    /// </summary>
    public ArgumentSpecBuilder? Argument { get; set; }

    /// <summary>
    /// Gets or sets the directive spec builder.
    /// </summary>
    public DirectiveSpecBuilder? Directive { get; set; }

    /// <summary>
    /// Gets a value indicating whether this member is associated with an option.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Option))]
    public bool IsOption => Option is not null;

    /// <summary>
    /// Gets a value indicating whether this member is associated with an argument.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Argument))]
    public bool IsArgument => Argument is not null;

    /// <summary>
    /// Gets a value indicating whether this member is associated with an directive.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Directive))]
    public bool IsDirective => Directive is not null;
}