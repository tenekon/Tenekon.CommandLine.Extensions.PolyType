namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;

internal sealed class FixedFunctionResolver<TFunction>(TFunction instance) : ICommandFunctionResolver
{
    public bool TryResolve<TInnerFunction>(out TInnerFunction value)
    {
        if (instance is TInnerFunction instance2)
        {
            value = instance2;
            return true;
        }

        value = default!;
        return false;
    }
}