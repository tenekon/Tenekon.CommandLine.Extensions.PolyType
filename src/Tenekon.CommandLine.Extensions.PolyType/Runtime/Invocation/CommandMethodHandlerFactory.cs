using System.CommandLine;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Binding;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;

internal static class CommandMethodHandlerFactory
{
    public static CommandHandler TryCreateHandler(
        IMethodShape methodShape,
        MethodParameterBindingInfo[] parameterBindings,
        BindingContext bindingContext,
        CommandRuntimeSettings settings)
    {
        if (parameterBindings.Length != methodShape.Parameters.Count)
            throw new InvalidOperationException(
                $"Unable to bind parameters for '{methodShape.DeclaringType.Type.FullName}.{methodShape.Name}'.");

        if (!IsSupportedReturn(methodShape))
            throw new InvalidOperationException(
                $"Command method '{methodShape.DeclaringType.Type.FullName}.{methodShape.Name}' has an unsupported return type.");

        var declaringType = methodShape.DeclaringType.Type;
        var invoker = CreateInvoker(methodShape, parameterBindings, declaringType);

        Func<ParseResult, ICommandServiceResolver?, CancellationToken, Task<int>> invokeAsync =
            async (parseResult, serviceResolver, cancellationToken) =>
            {
                serviceResolver ??= bindingContext.CurrentServiceResolver ?? bindingContext.DefaultServiceResolver;
                var instance = bindingContext.Bind(
                    parseResult,
                    declaringType,
                    serviceResolver,
                    returnEmpty: false,
                    cancellationToken);
                var context = bindingContext.CreateRuntimeContext(parseResult, serviceResolver);

                if (settings.ShowHelpOnEmptyCommand && context.IsEmptyCommand())
                {
                    context.ShowHelp();
                    return 0;
                }

                return await invoker(instance, context, cancellationToken, serviceResolver)
                    .ConfigureAwait(continueOnCapturedContext: false);
            };

        Func<ParseResult, ICommandServiceResolver?, int> invoke = (parseResult, serviceResolver) =>
        {
            return invokeAsync(parseResult, serviceResolver, CancellationToken.None).GetAwaiter().GetResult();
        };

        return new CommandHandler(invoke, invokeAsync, methodShape.IsAsync);
    }

    private static bool IsSupportedReturn(IMethodShape method)
    {
        if (method.IsVoidLike) return true;
        return method.ReturnType.Type == typeof(int);
    }

    private static Func<object, CommandRuntimeContext, CancellationToken, ICommandServiceResolver?, Task<int>>
        CreateInvoker(IMethodShape methodShape, MethodParameterBindingInfo[] parameterBindings, Type definitionType)
    {
        var invoker =
            methodShape.Accept(new MethodInvokerFactory(), new MethodInvocationState(parameterBindings, definitionType))
                as Func<object, CommandRuntimeContext, CancellationToken, ICommandServiceResolver?, Task<int>>;
        if (invoker is null) throw new InvalidOperationException($"Unable to build handler for '{methodShape.Name}'.");

        return invoker;
    }

    private sealed class MethodInvokerFactory : TypeShapeVisitor
    {
        public override object? VisitMethod<TDeclaringType, TArgumentState, TResult>(
            IMethodShape<TDeclaringType, TArgumentState, TResult> methodShape,
            object? state = null)
        {
            var invocationState = (MethodInvocationState)state!;
            var bindings = invocationState.Bindings;
            var argumentStateCtor = methodShape.GetArgumentStateConstructor();
            var invoker = methodShape.GetMethodInvoker();

            var parameterSetters = methodShape.Parameters
                .Select((parameter, index) => (ArgumentStateSetter<TArgumentState>)parameter.Accept(
                    new ParameterSetterVisitor(),
                    new ParameterBindingState(bindings[index], invocationState.DefinitionType))!)
                .ToArray();

            return new
                Func<object, CommandRuntimeContext, CancellationToken, ICommandServiceResolver?, Task<int>>(async (
                    instance,
                    context,
                    cancellationToken,
                    serviceResolver) =>
                {
                    var argumentState = argumentStateCtor();
                    foreach (var setter in parameterSetters)
                        setter(ref argumentState, context, cancellationToken, serviceResolver);

                    var typedInstance = (TDeclaringType)instance;
                    var result = await invoker(ref typedInstance, ref argumentState)
                        .ConfigureAwait(continueOnCapturedContext: false);

                    if (methodShape.IsVoidLike) return 0;
                    if (typeof(TResult) == typeof(int)) return (int)(object)result!;

                    return 0;
                });
        }

        private delegate void ArgumentStateSetter<TArgumentState>(
            ref TArgumentState state,
            CommandRuntimeContext context,
            CancellationToken cancellationToken,
            ICommandServiceResolver? serviceResolver);

        private sealed class ParameterSetterVisitor : TypeShapeVisitor
        {
            public override object? VisitParameter<TArgumentState, TParameterType>(
                IParameterShape<TArgumentState, TParameterType> parameterShape,
                object? state = null)
            {
                var bindingState = (ParameterBindingState)state!;
                var binding = bindingState.Binding;
                var setter = parameterShape.GetSetter();

                ArgumentStateSetter<TArgumentState> handler = binding.Kind switch
                {
                    MethodParameterBindingKind.Context => (ref argumentState, context, _, _) => setter(
                        ref argumentState,
                        (TParameterType)(object)context),
                    MethodParameterBindingKind.CancellationToken => (ref argumentState, _, token, _) => setter(
                        ref argumentState,
                        (TParameterType)(object)token),
                    MethodParameterBindingKind.Option => (ref argumentState, context, _, _) =>
                    {
                        var option = (Option<TParameterType>)binding.Symbol!;
                        var value = context.ParseResult.GetValue(option);
                        if (value is null && !typeof(TParameterType).IsValueType) return;
                        setter(ref argumentState, value!);
                    },
                    MethodParameterBindingKind.Argument => (ref argumentState, context, _, _) =>
                    {
                        var argument = (Argument<TParameterType>)binding.Symbol!;
                        var value = context.ParseResult.GetValue(argument);
                        if (value is null && !typeof(TParameterType).IsValueType) return;
                        setter(ref argumentState, value!);
                    },
                    MethodParameterBindingKind.Directive => (ref argumentState, context, _, _) =>
                    {
                        var directive = (Directive)binding.Symbol!;
                        if (!DirectiveValueHelper.TryGetValue(
                                context.ParseResult,
                                directive,
                                out TParameterType value)) return;
                        setter(ref argumentState, value);
                    },
                    _ => (ref argumentState, context, cancellationToken, resolver) =>
                    {
                        var bindingContext = context.BindingContext;
                        if (bindingContext.TryResolveParentInstance(
                                context.ParseResult,
                                bindingState.DefinitionType,
                                typeof(TParameterType),
                                resolver,
                                cancellationToken,
                                out var parentInstance) && parentInstance is TParameterType typedParent)
                        {
                            setter(ref argumentState, typedParent);
                            return;
                        }

                        var value = ParameterResolution.ResolveOrDefault(
                            parameterShape,
                            bindingContext,
                            resolver,
                            context.FunctionResolver);
                        setter(ref argumentState, value);
                    }
                };

                return handler;
            }
        }

        private readonly record struct ParameterBindingState(MethodParameterBindingInfo Binding, Type DefinitionType);
    }

    private readonly record struct MethodInvocationState(MethodParameterBindingInfo[] Bindings, Type DefinitionType);
}