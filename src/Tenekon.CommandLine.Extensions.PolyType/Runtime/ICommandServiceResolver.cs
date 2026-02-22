namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

/// <summary>
/// Resolves services for command invocation.
/// </summary>
public interface ICommandServiceResolver
{
    /// <summary>
    /// Attempts to resolve a service instance.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="value">The resolved instance.</param>
    /// <returns><see langword="true" /> if an instance was resolved; otherwise <see langword="false" />.</returns>
    bool TryResolve<TService>(out TService? value);
}
