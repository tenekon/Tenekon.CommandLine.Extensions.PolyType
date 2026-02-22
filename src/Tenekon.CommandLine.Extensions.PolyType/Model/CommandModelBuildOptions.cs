namespace Tenekon.CommandLine.Extensions.PolyType.Model;

/// <summary>
/// Defines how root parent relationships are handled when building a model.
/// </summary>
public enum RootParentHandling
{
    /// <summary>Throw when a root node declares a parent.</summary>
    Throw,
    /// <summary>Ignore a declared parent on the root node.</summary>
    Ignore
}

/// <summary>
/// Options used when building a command model.
/// </summary>
public sealed record CommandModelBuildOptions
{
    /// <summary>Default build options.</summary>
    public static CommandModelBuildOptions Default { get; } = new();

    /// <summary>How to handle parent metadata on the root node.</summary>
    public RootParentHandling RootParentHandling { get; init; } = RootParentHandling.Throw;
}
