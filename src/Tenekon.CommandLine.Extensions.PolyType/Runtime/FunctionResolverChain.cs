namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

internal sealed class FunctionResolverChain(IReadOnlyList<ICommandFunctionResolver> resolvers)
    : ICommandFunctionResolver
{
    public bool TryResolve<TFunction>(out TFunction value)
    {
        foreach (var resolver in resolvers)
            if (resolver.TryResolve(out value))
                return true;

        value = default!;
        return false;
    }

    public static ICommandFunctionResolver? Create(IEnumerable<ICommandFunctionResolver> resolvers)
    {
        var list = resolvers.Where(static resolver => resolver is not null).ToList();

        if (list.Count == 0) return null;

        if (list.Count == 1) return list[index: 0];

        return new FunctionResolverChain(list);
    }
}

internal sealed class ServiceFunctionResolver(ICommandServiceResolver resolver) : ICommandFunctionResolver
{
    public bool TryResolve<TFunction>(out TFunction value)
    {
        if (resolver.TryResolve(out TFunction? resolved) && resolved is not null)
        {
            value = resolved;
            return true;
        }

        value = default!;
        return false;
    }
}