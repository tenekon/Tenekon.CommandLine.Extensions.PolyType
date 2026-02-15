using System.Collections.Concurrent;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

public sealed class CommandFunctionRegistry : ICommandFunctionResolver
{
    private readonly ConcurrentDictionary<Type, object> _instances = new();

    bool ICommandFunctionResolver.TryResolve<TFunction>(out TFunction value)
    {
        return TryGet(out value);
    }

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

    public TFunction GetOrAdd<TFunction>(Func<TFunction> factory)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));

        var instance = _instances.GetOrAdd(typeof(TFunction), _ => factory());
        return (TFunction)instance;
    }

    public void Set<TFunction>(TFunction instance)
    {
        if (instance is null) throw new ArgumentNullException(nameof(instance));
        _instances[typeof(TFunction)] = instance;
    }
}