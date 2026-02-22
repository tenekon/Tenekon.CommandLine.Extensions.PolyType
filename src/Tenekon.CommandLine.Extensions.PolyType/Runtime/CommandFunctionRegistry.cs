using System.Collections.Concurrent;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

/// <summary>
/// Registry for resolving function command handlers.
/// </summary>
public sealed class CommandFunctionRegistry : ICommandFunctionResolver
{
    private readonly ConcurrentDictionary<Type, object> _instances = new();

    bool ICommandFunctionResolver.TryResolve<TFunction>(out TFunction value)
    {
        return TryGet(out value);
    }

    /// <summary>
    /// Attempts to get a registered function instance.
    /// </summary>
    /// <typeparam name="TFunction">The function type.</typeparam>
    /// <param name="value">The resolved instance.</param>
    /// <returns><see langword="true" /> if an instance was found; otherwise <see langword="false" />.</returns>
    public bool TryGet<TFunction>(out TFunction value)
    {
        if (_instances.TryGetValue(typeof(TFunction), out var instance))
        {
            value = (TFunction)instance;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Gets a registered instance or adds one using the provided factory.
    /// </summary>
    /// <typeparam name="TFunction">The function type.</typeparam>
    /// <param name="factory">Factory used to create the instance.</param>
    /// <returns>The resolved instance.</returns>
    public TFunction GetOrAdd<TFunction>(Func<TFunction> factory)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));

        var instance = _instances.GetOrAdd(typeof(TFunction), _ => factory());
        return (TFunction)instance;
    }

    /// <summary>
    /// Registers a function instance.
    /// </summary>
    /// <typeparam name="TFunction">The function type.</typeparam>
    /// <param name="instance">The instance to register.</param>
    public void Set<TFunction>(TFunction instance)
    {
        if (instance is null) throw new ArgumentNullException(nameof(instance));
        _instances[typeof(TFunction)] = instance;
    }
}
