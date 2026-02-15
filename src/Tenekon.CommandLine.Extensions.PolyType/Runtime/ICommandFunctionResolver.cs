namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

public interface ICommandFunctionResolver
{
    bool TryResolve<TFunction>(out TFunction value);
}