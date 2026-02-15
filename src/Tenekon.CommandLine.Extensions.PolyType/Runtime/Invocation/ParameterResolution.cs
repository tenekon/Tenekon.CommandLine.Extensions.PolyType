using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Binding;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;

internal static class ParameterResolution
{
    public static TParameterType ResolveOrDefault<TArgumentState, TParameterType>(
        IParameterShape<TArgumentState, TParameterType> parameterShape,
        BindingContext bindingContext,
        ICommandServiceResolver? resolver,
        ICommandFunctionResolver? functionResolver)
    {
        object? resolved = null;
        var parameterType = typeof(TParameterType);
        var isFunctionType = typeof(Delegate).IsAssignableFrom(parameterType) && parameterType != typeof(Delegate);

        var resolvedFunctionResolver = functionResolver
            ?? bindingContext.CreateFunctionResolver(resolver, overrideResolver: null);
        if (bindingContext.TryResolveFunctionInstance<TParameterType>(
                resolvedFunctionResolver,
                out var functionInstance))
            resolved = functionInstance;
        else if (!isFunctionType && resolver is not null && resolver.TryResolve<TParameterType>(out var value))
            resolved = value;

        if (resolved is TParameterType typed) return typed;

        if (resolved is null)
        {
            if (parameterShape.HasDefaultValue) return parameterShape.DefaultValue!;
            if (!parameterShape.IsRequired) return default!;

            throw new InvalidOperationException($"Unable to resolve required parameter '{parameterShape.Name}'.");
        }

        return (TParameterType)resolved;
    }
}