namespace Tenekon.CommandLine.Extensions.PolyType.Spec;

/// <summary>
/// Declares option metadata for a property or parameter.
/// </summary>
/// <remarks>
/// Option metadata drives naming, help text, arity, validation, and parsing behavior.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class OptionSpecAttribute : Attribute
{
    private bool _required;
    private ArgumentArity _arity;

    /// <summary>Explicit option name; when null, a naming policy is used.</summary>
    public string? Name { get; set; }
    /// <summary>Help text shown in usage output.</summary>
    public string? Description { get; set; }
    /// <summary>Whether the option is hidden from help output.</summary>
    public bool Hidden { get; set; }
    /// <summary>Ordering value used for help layout.</summary>
    public int Order { get; set; }
    /// <summary>Single alias for the option.</summary>
    public string? Alias { get; set; }
    /// <summary>Additional aliases for the option.</summary>
    public string[]? Aliases { get; set; }
    /// <summary>Help name for the option argument.</summary>
    public string? HelpName { get; set; }
    /// <summary>Whether the option may be specified recursively (for subcommands).</summary>
    public bool Recursive { get; set; }

    /// <summary>
    /// Gets or sets the allowed arity for this option.
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
    /// <summary>Whether multiple arguments can be provided in a single token.</summary>
    public bool AllowMultipleArgumentsPerToken { get; set; }

    /// <summary>
    /// Gets or sets whether the option is required.
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
