using System.CommandLine;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Binding;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal sealed class ConstructorFactory : TypeShapeVisitor
{
    public override object? VisitConstructor<TDeclaringType, TArgumentState>(
        IConstructorShape<TDeclaringType, TArgumentState> constructorShape,
        object? state = null)
    {
        if (constructorShape.Parameters.Count == 0)
        {
            var defaultCtor = constructorShape.GetDefaultConstructor();
            return new Func<BindingContext, ParseResult, ICommandServiceResolver?, CancellationToken, object>((
                _,
                _,
                _,
                _) => defaultCtor()!);
        }

        var argumentStateCtor = constructorShape.GetArgumentStateConstructor();
        var ctor = constructorShape.GetParameterizedConstructor();

        var parameterSetters = constructorShape.Parameters
                // Learning: we only must cover constructor parameters that are part of the method signature
            .Where(x => x.Kind is ParameterKind.MethodParameter)
            .Select(parameter => (ArgumentStateSetter<TArgumentState>)parameter.Accept(
                new ParameterSetterVisitor(),
                typeof(TDeclaringType))!)
            .ToArray();

        return new Func<BindingContext, ParseResult, ICommandServiceResolver?, CancellationToken, object>((
            bindingContext,
            parseResult,
            serviceResolver,
            cancellationToken) =>
        {
            var functionResolver = bindingContext.GetInvocationFunctionResolver(serviceResolver)
                ?? bindingContext.CreateFunctionResolver(serviceResolver, overrideResolver: null);
            var argumentState = argumentStateCtor();
            foreach (var setter in parameterSetters)
                setter(
                    ref argumentState,
                    bindingContext,
                    parseResult,
                    serviceResolver,
                    functionResolver,
                    cancellationToken);

            var instance = ctor(ref argumentState);
            return instance!;
        });
    }

    private delegate void ArgumentStateSetter<TArgumentState>(
        ref TArgumentState state,
        BindingContext bindingContext,
        ParseResult parseResult,
        ICommandServiceResolver? resolver,
        ICommandFunctionResolver? functionResolver,
        CancellationToken cancellationToken);

    private sealed class ParameterSetterVisitor : TypeShapeVisitor
    {
        public override object? VisitParameter<TArgumentState, TParameterType>(
            IParameterShape<TArgumentState, TParameterType> parameterShape,
            object? state = null)
        {
            var definitionType = (Type)state!;
            var setter = parameterShape.GetSetter();

            return new ArgumentStateSetter<TArgumentState>((
                ref argumentState,
                bindingContext,
                parseResult,
                resolver,
                functionResolver,
                cancellationToken) =>
            {
                if (typeof(TParameterType) == typeof(CommandRuntimeContext))
                {
                    var context = bindingContext.CreateRuntimeContext(parseResult, resolver);
                    setter(ref argumentState, (TParameterType)(object)context);
                    return;
                }

                if (typeof(TParameterType) == typeof(CancellationToken))
                {
                    setter(ref argumentState, (TParameterType)(object)cancellationToken);
                    return;
                }

                if (bindingContext.TryResolveParentInstance(
                        parseResult,
                        definitionType,
                        typeof(TParameterType),
                        resolver,
                        cancellationToken,
                        out var parentInstance) && parentInstance is TParameterType typedParent)
                {
                    setter(ref argumentState, typedParent);
                    return;
                }

                object? value = null;
                var parameterType = typeof(TParameterType);
                var isFunctionType = typeof(Delegate).IsAssignableFrom(parameterType)
                    && parameterType != typeof(Delegate);
                var hasValue = false;

                if (bindingContext.TryResolveFunctionInstance<TParameterType>(
                        functionResolver,
                        out var functionInstance))
                {
                    value = functionInstance;
                    hasValue = true;
                }
                else if (!isFunctionType && resolver is not null
                         && resolver.TryResolve<TParameterType>(out var resolved))
                {
                    value = resolved;
                    hasValue = true;
                }

                if (!hasValue)
                {
                    if (parameterShape.HasDefaultValue)
                    {
                        value = parameterShape.DefaultValue!;
                        hasValue = true;
                    }
                    else if (!parameterShape.IsRequired)
                    {
                        value = default!;
                        hasValue = true;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Unable to resolve required constructor parameter '{parameterShape.Name}'.");
                    }
                }

                if (value is null && typeof(TParameterType).IsValueType)
                {
                    setter(ref argumentState, default!);
                    return;
                }

                setter(ref argumentState, (TParameterType)value!);
            });
        }
    }
}