using System.Collections.Immutable;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

/// <summary>
/// Immutable model describing an option specification.
/// </summary>
public sealed class OptionSpecModel
{
    /// <summary>Explicit option name; when null, a naming policy is used.</summary>
    public string? Name { get; internal set; }
    /// <summary>Help text shown in usage output.</summary>
    public string? Description { get; internal set; }
    /// <summary>Whether the option is hidden from help output.</summary>
    public bool Hidden { get; internal set; }
    /// <summary>Ordering value used for help layout.</summary>
    public int Order { get; internal set; }
    /// <summary>Single alias for the option.</summary>
    public string? Alias { get; internal set; }
    /// <summary>Additional aliases for the option.</summary>
    public ImmutableArray<string> Aliases { get; internal set; } = ImmutableArray<string>.Empty;
    /// <summary>Help name for the option argument.</summary>
    public string? HelpName { get; internal set; }
    /// <summary>Whether the option may be specified recursively.</summary>
    public bool Recursive { get; internal set; }

    /// <summary>Allowed arity for the option.</summary>
    public ArgumentArity Arity { get; internal set; }
    /// <summary>Whether the arity was explicitly specified.</summary>
    public bool IsAritySpecified { get; internal set; }

    /// <summary>Optional list of allowed values.</summary>
    public ImmutableArray<string> AllowedValues { get; internal set; } = ImmutableArray<string>.Empty;
    /// <summary>Built-in validation rules to apply.</summary>
    public ValidationRules ValidationRules { get; internal set; }
    /// <summary>Regex pattern for custom validation.</summary>
    public string? ValidationPattern { get; internal set; }
    /// <summary>Custom validation message for pattern failures.</summary>
    public string? ValidationMessage { get; internal set; }
    /// <summary>Whether multiple arguments can be provided in a single token.</summary>
    public bool AllowMultipleArgumentsPerToken { get; internal set; }

    /// <summary>Whether the option is required.</summary>
    public bool Required { get; internal set; }
    /// <summary>Whether the required flag was explicitly specified.</summary>
    public bool IsRequiredSpecified { get; internal set; }

    internal static OptionSpecModel FromAttribute(OptionSpecAttribute spec)
    {
        return new OptionSpecModel
        {
            Name = spec.Name,
            Description = spec.Description,
            Hidden = spec.Hidden,
            Order = spec.Order,
            Alias = spec.Alias,
            Aliases = spec.Aliases is { Length: > 0 } ? [..spec.Aliases] : ImmutableArray<string>.Empty,
            HelpName = spec.HelpName,
            Recursive = spec.Recursive,
            Arity = spec.Arity,
            IsAritySpecified = spec.IsAritySpecified,
            AllowedValues = spec.AllowedValues is { Length: > 0 }
                ? [..spec.AllowedValues]
                : ImmutableArray<string>.Empty,
            ValidationRules = spec.ValidationRules,
            ValidationPattern = spec.ValidationPattern,
            ValidationMessage = spec.ValidationMessage,
            AllowMultipleArgumentsPerToken = spec.AllowMultipleArgumentsPerToken,
            Required = spec.Required,
            IsRequiredSpecified = spec.IsRequiredSpecified
        };
    }

    internal OptionSpecModel Clone()
    {
        return new OptionSpecModel
        {
            Name = Name,
            Description = Description,
            Hidden = Hidden,
            Order = Order,
            Alias = Alias,
            Aliases = Aliases,
            HelpName = HelpName,
            Recursive = Recursive,
            Arity = Arity,
            IsAritySpecified = IsAritySpecified,
            AllowedValues = AllowedValues,
            ValidationRules = ValidationRules,
            ValidationPattern = ValidationPattern,
            ValidationMessage = ValidationMessage,
            AllowMultipleArgumentsPerToken = AllowMultipleArgumentsPerToken,
            Required = Required,
            IsRequiredSpecified = IsRequiredSpecified
        };
    }
}
