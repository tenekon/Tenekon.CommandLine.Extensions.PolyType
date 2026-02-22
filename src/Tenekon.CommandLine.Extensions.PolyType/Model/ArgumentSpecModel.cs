using System.Collections.Immutable;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

/// <summary>
/// Immutable model describing an argument specification.
/// </summary>
public sealed class ArgumentSpecModel
{
    /// <summary>Explicit argument name; when null, a naming policy is used.</summary>
    public string? Name { get; internal set; }
    /// <summary>Help text shown in usage output.</summary>
    public string? Description { get; internal set; }
    /// <summary>Whether the argument is hidden from help output.</summary>
    public bool Hidden { get; internal set; }
    /// <summary>Ordering value used for help layout.</summary>
    public int Order { get; internal set; }
    /// <summary>Help name for the argument.</summary>
    public string? HelpName { get; internal set; }

    /// <summary>Allowed arity for the argument.</summary>
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

    /// <summary>Whether the argument is required.</summary>
    public bool Required { get; internal set; }
    /// <summary>Whether the required flag was explicitly specified.</summary>
    public bool IsRequiredSpecified { get; internal set; }

    internal static ArgumentSpecModel FromAttribute(ArgumentSpecAttribute spec)
    {
        return new ArgumentSpecModel
        {
            Name = spec.Name,
            Description = spec.Description,
            Hidden = spec.Hidden,
            Order = spec.Order,
            HelpName = spec.HelpName,
            Arity = spec.Arity,
            IsAritySpecified = spec.IsAritySpecified,
            AllowedValues = spec.AllowedValues is { Length: > 0 }
                ? [..spec.AllowedValues]
                : ImmutableArray<string>.Empty,
            ValidationRules = spec.ValidationRules,
            ValidationPattern = spec.ValidationPattern,
            ValidationMessage = spec.ValidationMessage,
            Required = spec.Required,
            IsRequiredSpecified = spec.IsRequiredSpecified
        };
    }

    internal ArgumentSpecModel Clone()
    {
        return new ArgumentSpecModel
        {
            Name = Name,
            Description = Description,
            Hidden = Hidden,
            Order = Order,
            HelpName = HelpName,
            Arity = Arity,
            IsAritySpecified = IsAritySpecified,
            AllowedValues = AllowedValues,
            ValidationRules = ValidationRules,
            ValidationPattern = ValidationPattern,
            ValidationMessage = ValidationMessage,
            Required = Required,
            IsRequiredSpecified = IsRequiredSpecified
        };
    }
}
