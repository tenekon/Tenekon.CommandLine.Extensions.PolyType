using System.Collections.Immutable;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

/// <summary>
/// Immutable model describing handler method discovery conventions.
/// </summary>
public sealed class CommandHandlerConventionSpecModel
{
    internal static ImmutableArray<string> DefaultMethodNames { get; } = ["RunAsync", "Run"];

    /// <summary>Ordered list of method names to consider as handlers.</summary>
    public ImmutableArray<string> MethodNames { get; internal set; } = DefaultMethodNames;
    /// <summary>Whether async methods are preferred when both async and sync handlers are available.</summary>
    public bool PreferAsync { get; internal set; } = true;
    /// <summary>Whether handler discovery is disabled for the command.</summary>
    public bool Disabled { get; internal set; }

    internal static CommandHandlerConventionSpecModel CreateDefault()
    {
        return new CommandHandlerConventionSpecModel
        {
            MethodNames = DefaultMethodNames,
            PreferAsync = true,
            Disabled = false
        };
    }

    internal CommandHandlerConventionSpecModel Clone()
    {
        return new CommandHandlerConventionSpecModel
        {
            MethodNames = MethodNames,
            PreferAsync = PreferAsync,
            Disabled = Disabled
        };
    }
}
