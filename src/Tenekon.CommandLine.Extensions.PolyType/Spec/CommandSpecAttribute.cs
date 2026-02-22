namespace Tenekon.CommandLine.Extensions.PolyType.Spec;

/// <summary>
/// Declares command metadata for a type, delegate, or method.
/// </summary>
/// <remarks>
/// Command metadata controls naming, aliases, hierarchy, and parsing behavior.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Delegate, Inherited = false)]
public sealed class CommandSpecAttribute : Attribute
{
    private NameAutoGenerate _nameAutoGenerate = NameAutoGenerate.All;
    private NameCasingConvention _nameCasingConvention = NameCasingConvention.KebabCase;
    private NamePrefixConvention _namePrefixConvention = NamePrefixConvention.DoubleHyphen;
    private NameAutoGenerate _shortFormAutoGenerate = NameAutoGenerate.All;
    private NamePrefixConvention _shortFormPrefixConvention = NamePrefixConvention.SingleHyphen;

    /// <summary>Explicit command name; when null, a naming policy is used.</summary>
    public string? Name { get; set; }
    /// <summary>Help text shown in usage output.</summary>
    public string? Description { get; set; }
    /// <summary>Whether the command is hidden from help output.</summary>
    public bool Hidden { get; set; }
    /// <summary>Ordering value used for help layout.</summary>
    public int Order { get; set; }
    /// <summary>Single alias for the command.</summary>
    public string? Alias { get; set; }
    /// <summary>Additional aliases for the command.</summary>
    public string[]? Aliases { get; set; }
    /// <summary>Explicit parent command type.</summary>
    public Type? Parent { get; set; }
    /// <summary>Explicit child command types.</summary>
    public Type[]? Children { get; set; }
    /// <summary>Whether unmatched tokens should be reported as errors.</summary>
    public bool TreatUnmatchedTokensAsErrors { get; set; } = true;

    /// <summary>Controls auto-generation for the command name.</summary>
    public NameAutoGenerate NameAutoGenerate
    {
        get => _nameAutoGenerate;
        set
        {
            _nameAutoGenerate = value;
            IsNameAutoGenerateSpecified = true;
        }
    }

    /// <summary>Applies casing conventions to generated names.</summary>
    public NameCasingConvention NameCasingConvention
    {
        get => _nameCasingConvention;
        set
        {
            _nameCasingConvention = value;
            IsNameCasingConventionSpecified = true;
        }
    }

    /// <summary>Applies prefix conventions to generated names.</summary>
    public NamePrefixConvention NamePrefixConvention
    {
        get => _namePrefixConvention;
        set
        {
            _namePrefixConvention = value;
            IsNamePrefixConventionSpecified = true;
        }
    }

    /// <summary>Controls auto-generation for short form names.</summary>
    public NameAutoGenerate ShortFormAutoGenerate
    {
        get => _shortFormAutoGenerate;
        set
        {
            _shortFormAutoGenerate = value;
            IsShortFormAutoGenerateSpecified = true;
        }
    }

    /// <summary>Applies prefix conventions to generated short form names.</summary>
    public NamePrefixConvention ShortFormPrefixConvention
    {
        get => _shortFormPrefixConvention;
        set
        {
            _shortFormPrefixConvention = value;
            IsShortFormPrefixConventionSpecified = true;
        }
    }

    internal bool IsNameAutoGenerateSpecified { get; private set; }
    internal bool IsNameCasingConventionSpecified { get; private set; }
    internal bool IsNamePrefixConventionSpecified { get; private set; }
    internal bool IsShortFormAutoGenerateSpecified { get; private set; }
    internal bool IsShortFormPrefixConventionSpecified { get; private set; }
}
