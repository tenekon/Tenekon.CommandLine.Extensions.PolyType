using System.Diagnostics.CodeAnalysis;
using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

/// <summary>
/// Describes CLI specs for a command member (property).
/// </summary>
public sealed class CommandMemberSpecBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandMemberSpecBuilder"/> class.
    /// </summary>
    /// <param name="ownerType">The type that declares the member.</param>
    /// <param name="specProperty">The property that contributes CLI specs.</param>
    /// <param name="targetProperty">The property that receives bound values.</param>
    /// <param name="option">The option spec builder, if any.</param>
    /// <param name="argument">The argument spec builder, if any.</param>
    /// <param name="directive">The directive spec builder, if any.</param>
    public CommandMemberSpecBuilder(
        Type ownerType,
        IPropertyShape specProperty,
        IPropertyShape targetProperty,
        OptionSpecBuilder? option,
        ArgumentSpecBuilder? argument,
        DirectiveSpecBuilder? directive)
    {
        OwnerType = ownerType;
        SpecProperty = specProperty;
        TargetProperty = targetProperty;
        Option = option;
        Argument = argument;
        Directive = directive;
    }

    /// <summary>
    /// Gets the type that declares the spec property.
    /// </summary>
    public Type OwnerType { get; }

    /// <summary>
    /// Gets the property that provides the CLI spec metadata.
    /// </summary>
    public IPropertyShape SpecProperty { get; }

    /// <summary>
    /// Gets the property that receives bound values.
    /// </summary>
    public IPropertyShape TargetProperty { get; }

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
    
}
