using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Binding;

internal readonly record struct FunctionResolveResult(bool Success, object? Value);

internal sealed class FunctionResolveInvoker : TypeShapeVisitor
{
    public static readonly FunctionResolveInvoker Instance = new();

    public FunctionResolveResult Resolve(IFunctionTypeShape functionShape, ICommandFunctionResolver resolver)
    {
        return (FunctionResolveResult)functionShape.Accept(Instance, resolver)!;
    }

    public override object? VisitFunction<TFunction, TArgumentState, TResult>(
        IFunctionTypeShape<TFunction, TArgumentState, TResult> functionShape,
        object? state = null)
    {
        var resolver = (ICommandFunctionResolver)state!;
        if (resolver.TryResolve<TFunction>(out var instance) && instance is not null)
            return new FunctionResolveResult(Success: true, instance);

        return new FunctionResolveResult(Success: false, Value: null);
    }
}