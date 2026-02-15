namespace Tenekon.CommandLine.Extensions.PolyType.Spec;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class DirectiveSpecAttribute : Attribute
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool Hidden { get; set; }
    public int Order { get; set; }
}