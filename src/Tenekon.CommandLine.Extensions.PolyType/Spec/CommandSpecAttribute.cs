namespace Tenekon.CommandLine.Extensions.PolyType.Spec;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Delegate, Inherited = false)]
public sealed class CommandSpecAttribute : Attribute
{
    private NameAutoGenerate _nameAutoGenerate = NameAutoGenerate.All;
    private NameCasingConvention _nameCasingConvention = NameCasingConvention.KebabCase;
    private NamePrefixConvention _namePrefixConvention = NamePrefixConvention.DoubleHyphen;
    private NameAutoGenerate _shortFormAutoGenerate = NameAutoGenerate.All;
    private NamePrefixConvention _shortFormPrefixConvention = NamePrefixConvention.SingleHyphen;

    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool Hidden { get; set; }
    public int Order { get; set; }
    public string? Alias { get; set; }
    public string[]? Aliases { get; set; }
    public Type? Parent { get; set; }
    public Type[]? Children { get; set; }
    public bool TreatUnmatchedTokensAsErrors { get; set; } = true;

    public NameAutoGenerate NameAutoGenerate
    {
        get => _nameAutoGenerate;
        set
        {
            _nameAutoGenerate = value;
            IsNameAutoGenerateSpecified = true;
        }
    }

    public NameCasingConvention NameCasingConvention
    {
        get => _nameCasingConvention;
        set
        {
            _nameCasingConvention = value;
            IsNameCasingConventionSpecified = true;
        }
    }

    public NamePrefixConvention NamePrefixConvention
    {
        get => _namePrefixConvention;
        set
        {
            _namePrefixConvention = value;
            IsNamePrefixConventionSpecified = true;
        }
    }

    public NameAutoGenerate ShortFormAutoGenerate
    {
        get => _shortFormAutoGenerate;
        set
        {
            _shortFormAutoGenerate = value;
            IsShortFormAutoGenerateSpecified = true;
        }
    }

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