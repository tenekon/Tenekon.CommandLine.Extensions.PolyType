namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

internal interface IFunctionResolverAccessor
{
    ICommandFunctionResolver? FunctionResolver { get; }
}

internal sealed class CommandInvocationServiceResolver(
    ICommandServiceResolver? inner,
    ICommandFunctionResolver? functionResolver) : ICommandServiceResolver, IFunctionResolverAccessor
{
    public ICommandFunctionResolver? FunctionResolver { get; } = functionResolver;

    public bool TryResolve<TService>(out TService? value)
    {
        if (inner is not null)
        {
            return inner.TryResolve(out value);
        }

        value = default;
        return false;
    }
}
