namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

/// <summary>
/// Resolves function command handler instances.
/// </summary>
public interface ICommandFunctionResolver
{
    /// <summary>
    /// Attempts to resolve a function instance.
    /// </summary>
    /// <typeparam name="TFunction">The function type.</typeparam>
    /// <param name="value">The resolved instance.</param>
    /// <returns><see langword="true" /> if an instance was resolved; otherwise <see langword="false" />.</returns>
    bool TryResolve<TFunction>(out TFunction value);
}
