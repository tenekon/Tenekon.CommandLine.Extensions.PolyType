using System.Collections.Immutable;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

/// <summary>
/// Immutable model describing a command specification.
/// </summary>
public sealed class CommandSpecModel
{
    /// <summary>Explicit command name; when null, a naming policy is used.</summary>
    public string? Name { get; internal set; }
    /// <summary>Help text shown in usage output.</summary>
    public string? Description { get; internal set; }
    /// <summary>Whether the command is hidden from help output.</summary>
    public bool Hidden { get; internal set; }
    /// <summary>Ordering value used for help layout.</summary>
    public int Order { get; internal set; }
    /// <summary>Single alias for the command.</summary>
    public string? Alias { get; internal set; }
    /// <summary>Additional aliases for the command.</summary>
    public ImmutableArray<string> Aliases { get; internal set; } = ImmutableArray<string>.Empty;
    /// <summary>Explicit parent command type.</summary>
    public Type? Parent { get; internal set; }
    /// <summary>Explicit child command types.</summary>
    public ImmutableArray<Type> Children { get; internal set; } = ImmutableArray<Type>.Empty;
    /// <summary>Whether unmatched tokens should be reported as errors.</summary>
    public bool TreatUnmatchedTokensAsErrors { get; internal set; } = true;

    /// <summary>Controls auto-generation for the command name.</summary>
    public NameAutoGenerate NameAutoGenerate { get; internal set; } = NameAutoGenerate.All;
    /// <summary>Applies casing conventions to generated names.</summary>
    public NameCasingConvention NameCasingConvention { get; internal set; } = NameCasingConvention.KebabCase;
    /// <summary>Applies prefix conventions to generated names.</summary>
    public NamePrefixConvention NamePrefixConvention { get; internal set; } = NamePrefixConvention.DoubleHyphen;
    /// <summary>Controls auto-generation for short form names.</summary>
    public NameAutoGenerate ShortFormAutoGenerate { get; internal set; } = NameAutoGenerate.All;
    /// <summary>Applies prefix conventions to generated short form names.</summary>
    public NamePrefixConvention ShortFormPrefixConvention { get; internal set; } = NamePrefixConvention.SingleHyphen;

    /// <summary>Whether NameAutoGenerate was explicitly specified.</summary>
    public bool IsNameAutoGenerateSpecified { get; internal set; }
    /// <summary>Whether NameCasingConvention was explicitly specified.</summary>
    public bool IsNameCasingConventionSpecified { get; internal set; }
    /// <summary>Whether NamePrefixConvention was explicitly specified.</summary>
    public bool IsNamePrefixConventionSpecified { get; internal set; }
    /// <summary>Whether ShortFormAutoGenerate was explicitly specified.</summary>
    public bool IsShortFormAutoGenerateSpecified { get; internal set; }
    /// <summary>Whether ShortFormPrefixConvention was explicitly specified.</summary>
    public bool IsShortFormPrefixConventionSpecified { get; internal set; }

    internal static CommandSpecModel FromAttribute(CommandSpecAttribute spec)
    {
        var model = new CommandSpecModel
        {
            Name = spec.Name,
            Description = spec.Description,
            Hidden = spec.Hidden,
            Order = spec.Order,
            Alias = spec.Alias,
            Aliases = spec.Aliases is { Length: > 0 } ? [..spec.Aliases] : ImmutableArray<string>.Empty,
            Parent = spec.Parent,
            Children = spec.Children is { Length: > 0 }
                ? [..spec.Children.Where(child => child is not null).Cast<Type>()]
                : ImmutableArray<Type>.Empty,
            TreatUnmatchedTokensAsErrors = spec.TreatUnmatchedTokensAsErrors,
            NameAutoGenerate = spec.NameAutoGenerate,
            NameCasingConvention = spec.NameCasingConvention,
            NamePrefixConvention = spec.NamePrefixConvention,
            ShortFormAutoGenerate = spec.ShortFormAutoGenerate,
            ShortFormPrefixConvention = spec.ShortFormPrefixConvention,
            IsNameAutoGenerateSpecified = spec.IsNameAutoGenerateSpecified,
            IsNameCasingConventionSpecified = spec.IsNameCasingConventionSpecified,
            IsNamePrefixConventionSpecified = spec.IsNamePrefixConventionSpecified,
            IsShortFormAutoGenerateSpecified = spec.IsShortFormAutoGenerateSpecified,
            IsShortFormPrefixConventionSpecified = spec.IsShortFormPrefixConventionSpecified
        };

        return model;
    }

    internal CommandSpecModel Clone()
    {
        return new CommandSpecModel
        {
            Name = Name,
            Description = Description,
            Hidden = Hidden,
            Order = Order,
            Alias = Alias,
            Aliases = Aliases,
            Parent = Parent,
            Children = Children,
            TreatUnmatchedTokensAsErrors = TreatUnmatchedTokensAsErrors,
            NameAutoGenerate = NameAutoGenerate,
            NameCasingConvention = NameCasingConvention,
            NamePrefixConvention = NamePrefixConvention,
            ShortFormAutoGenerate = ShortFormAutoGenerate,
            ShortFormPrefixConvention = ShortFormPrefixConvention,
            IsNameAutoGenerateSpecified = IsNameAutoGenerateSpecified,
            IsNameCasingConventionSpecified = IsNameCasingConventionSpecified,
            IsNamePrefixConventionSpecified = IsNamePrefixConventionSpecified,
            IsShortFormAutoGenerateSpecified = IsShortFormAutoGenerateSpecified,
            IsShortFormPrefixConventionSpecified = IsShortFormPrefixConventionSpecified
        };
    }
}
