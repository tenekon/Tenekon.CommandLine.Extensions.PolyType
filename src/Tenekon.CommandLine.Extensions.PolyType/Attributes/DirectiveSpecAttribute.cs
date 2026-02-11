namespace Tenekon.CommandLine.Extensions.PolyType;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class DirectiveSpecAttribute : Attribute
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool Hidden { get; set; }
    public int Order { get; set; }
}