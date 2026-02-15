namespace Tenekon.CommandLine.Extensions.PolyType.Model;

public enum RootParentHandling
{
    Throw,
    Ignore
}

public sealed record CommandModelBuildOptions
{
    public static CommandModelBuildOptions Default { get; } = new();

    public RootParentHandling RootParentHandling { get; init; } = RootParentHandling.Throw;
}