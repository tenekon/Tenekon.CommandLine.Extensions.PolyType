namespace Tenekon.CommandLine.Extensions.PolyType.Spec;

/// <summary>
/// Declares directive metadata for a property or parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class DirectiveSpecAttribute : Attribute
{
    /// <summary>Explicit directive name; when null, a naming policy is used.</summary>
    public string? Name { get; set; }
    /// <summary>Help text shown in usage output.</summary>
    public string? Description { get; set; }
    /// <summary>Whether the directive is hidden from help output.</summary>
    public bool Hidden { get; set; }
    /// <summary>Ordering value used for help layout.</summary>
    public int Order { get; set; }
}
