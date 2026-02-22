namespace Tenekon.CommandLine.Extensions.PolyType.Spec;

/// <summary>
/// Declares argument metadata for a property or parameter.
/// </summary>
/// <remarks>
/// Argument metadata controls naming, help text, arity, and validation.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ArgumentSpecAttribute : Attribute
{
    private bool _required;
    private ArgumentArity _arity;

    /// <summary>Explicit argument name; when null, a naming policy is used.</summary>
    public string? Name { get; set; }
    /// <summary>Help text shown in usage output.</summary>
    public string? Description { get; set; }
    /// <summary>Whether the argument is hidden from help output.</summary>
    public bool Hidden { get; set; }
    /// <summary>Ordering value used for help layout.</summary>
    public int Order { get; set; }
    /// <summary>Help name for the argument.</summary>
    public string? HelpName { get; set; }

    /// <summary>
    /// Gets or sets the allowed arity for this argument.
    /// </summary>
    public ArgumentArity Arity
    {
        get => _arity;
        set
        {
            _arity = value;
            IsAritySpecified = true;
        }
    }

    /// <summary>Optional list of allowed values.</summary>
    public string[]? AllowedValues { get; set; }
    /// <summary>Built-in validation rules to apply.</summary>
    public ValidationRules ValidationRules { get; set; }
    /// <summary>Regex pattern for custom validation.</summary>
    public string? ValidationPattern { get; set; }
    /// <summary>Custom validation message for pattern failures.</summary>
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// Gets or sets whether the argument is required.
    /// </summary>
    public bool Required
    {
        get => _required;
        set
        {
            _required = value;
            IsRequiredSpecified = true;
        }
    }

    internal bool IsRequiredSpecified { get; private set; }
    internal bool IsAritySpecified { get; private set; }
}
