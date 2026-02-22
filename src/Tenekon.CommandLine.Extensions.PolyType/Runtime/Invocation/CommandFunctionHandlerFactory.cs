using System.CommandLine;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Binding;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;

internal static class CommandFunctionHandlerFactory
{
    public static CommandHandler TryCreateHandler(
        IFunctionTypeShape functionShape,
        MethodParameterBindingInfo[] parameterBindings,
        BindingContext bindingContext,
        CommandRuntimeSettings settings)
    {
        if (parameterBindings.Length != functionShape.Parameters.Count)
            throw new InvalidOperationException($"Unable to bind parameters for '{functionShape.Type.FullName}'.");

        if (!IsSupportedReturn(functionShape))
            throw new InvalidOperationException(
                $"Command function '{functionShape.Type.FullName}' has an unsupported return type.");

        var functionType = functionShape.Type;
        var invoker = CreateInvoker(functionShape, parameterBindings, functionType);

        return new CommandHandler(Invoke, InvokeAsync, functionShape.IsAsync);

        async Task<int> InvokeAsync(
            ParseResult parseResult,
            ICommandServiceResolver? serviceResolver,
            CancellationToken cancellationToken)
        {
            serviceResolver ??= bindingContext.CurrentServiceResolver ?? bindingContext.DefaultServiceResolver;
            var context = bindingContext.CreateRuntimeContext(parseResult, serviceResolver);

            if (!bindingContext.TryResolveFunctionInstance(functionShape, context.FunctionResolver, out var instance)
                || instance is null)
                throw new InvalidOperationException(
                    $"Function instance is not registered for '{functionType.FullName}'.");

            if (settings.ShowHelpOnEmptyCommand && context.IsEmptyCommand())
            {
                context.ShowHelp();
                return 0;
            }

            return await invoker(instance, context, cancellationToken, serviceResolver)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        int Invoke(ParseResult parseResult, ICommandServiceResolver? serviceResolver) =>
            InvokeAsync(parseResult, serviceResolver, CancellationToken.None).GetAwaiter().GetResult();
    }

    private static bool IsSupportedReturn(IFunctionTypeShape functionShape)
    {
        if (functionShape.IsVoidLike) return true;
        return functionShape.ReturnType.Type == typeof(int);
    }

    private static Func<object, CommandRuntimeContext, CancellationToken, ICommandServiceResolver?, Task<int>>
        CreateInvoker(
            IFunctionTypeShape functionShape,
            MethodParameterBindingInfo[] parameterBindings,
            Type definitionType)
    {
        var invoker =
            functionShape.Accept(
                new FunctionInvokerFactory(),
                new MethodInvocationState(parameterBindings, definitionType)) as Func<object, CommandRuntimeContext,
                CancellationToken, ICommandServiceResolver?, Task<int>>;
        if (invoker is null)
            throw new InvalidOperationException($"Unable to build handler for '{functionShape.Type.FullName}'.");

        return invoker;
    }

    private sealed class FunctionInvokerFactory : TypeShapeVisitor
    {
        public override object? VisitFunction<TFunction, TArgumentState, TResult>(
            IFunctionTypeShape<TFunction, TArgumentState, TResult> functionShape,
            object? state = null)
        {
            var invocationState = (MethodInvocationState)state!;
            var bindings = invocationState.Bindings;
            var argumentStateCtor = functionShape.GetArgumentStateConstructor();
            var invoker = functionShape.GetFunctionInvoker();

            var parameterSetters = functionShape.Parameters
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

                    var typedInstance = (TFunction)instance;
                    var result = await invoker(ref typedInstance, ref argumentState)
                        .ConfigureAwait(continueOnCapturedContext: false);

                    if (functionShape.IsVoidLike) return 0;
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
                var (binding, definitionType) = (ParameterBindingState)state!;
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
                                definitionType,
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