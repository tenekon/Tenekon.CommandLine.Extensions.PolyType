using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

/// <summary>
/// Immutable model describing a directive specification.
/// </summary>
public sealed class DirectiveSpecModel
{
    /// <summary>Explicit directive name; when null, a naming policy is used.</summary>
    public string? Name { get; internal set; }
    /// <summary>Help text shown in usage output.</summary>
    public string? Description { get; internal set; }
    /// <summary>Whether the directive is hidden from help output.</summary>
    public bool Hidden { get; internal set; }
    /// <summary>Ordering value used for help layout.</summary>
    public int Order { get; internal set; }

    internal static DirectiveSpecModel FromAttribute(DirectiveSpecAttribute spec)
    {
        return new DirectiveSpecModel
        {
            Name = spec.Name,
            Description = spec.Description,
            Hidden = spec.Hidden,
            Order = spec.Order
        };
    }

    internal DirectiveSpecModel Clone()
    {
        return new DirectiveSpecModel
        {
            Name = Name,
            Description = Description,
            Hidden = Hidden,
            Order = Order
        };
    }
}
