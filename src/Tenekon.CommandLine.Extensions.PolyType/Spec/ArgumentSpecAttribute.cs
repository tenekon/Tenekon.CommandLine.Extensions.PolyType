namespace Tenekon.CommandLine.Extensions.PolyType.Spec;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ArgumentSpecAttribute : Attribute
{
    private bool _required;
    private ArgumentArity _arity;

    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool Hidden { get; set; }
    public int Order { get; set; }
    public string? HelpName { get; set; }

    public ArgumentArity Arity
    {
        get => _arity;
        set
        {
            _arity = value;
            IsAritySpecified = true;
        }
    }

    public string[]? AllowedValues { get; set; }
    public ValidationRules ValidationRules { get; set; }
    public string? ValidationPattern { get; set; }
    public string? ValidationMessage { get; set; }

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