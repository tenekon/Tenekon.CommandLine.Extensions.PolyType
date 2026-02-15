namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

public interface ICommandServiceResolver
{
    bool TryResolve<TService>(out TService? value);
}