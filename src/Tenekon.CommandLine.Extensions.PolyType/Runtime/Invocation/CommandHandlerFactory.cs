using System.CommandLine;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Binding;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;

internal static class CommandHandlerFactory
{
    public static CommandHandler? TryCreateHandler(
        IObjectTypeShape shape,
        BindingContext bindingContext,
        CommandRuntimeSettings settings)
    {
        var methods = shape.Methods;
        var (asyncMethod, asyncInfo) = FindMethod(methods, "RunAsync", isAsync: true);
        var (syncMethod, syncInfo) = FindMethod(methods, "Run", isAsync: false);

        var selected = asyncMethod ?? syncMethod;
        if (selected is null) return CreateDefaultHelpHandler(bindingContext, settings);

        var info = asyncMethod is not null ? asyncInfo : syncInfo;
        if (info is null) return CreateDefaultHelpHandler(bindingContext, settings);

        var invoker = CreateInvoker(selected, info.Value, shape.Type);

        Func<ParseResult, ICommandServiceResolver?, CancellationToken, Task<int>> invokeAsync =
            async (parseResult, serviceResolver, cancellationToken) =>
            {
                serviceResolver ??= bindingContext.CurrentServiceResolver ?? bindingContext.DefaultServiceResolver;
                var instance = bindingContext.Bind(
                    parseResult,
                    shape.Type,
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

        return new CommandHandler(invoke, invokeAsync, asyncMethod is not null);
    }

    private static (IMethodShape? Method, MethodInvocationInfo? Info) FindMethod(
        IReadOnlyList<IMethodShape> methods,
        string name,
        bool isAsync)
    {
        foreach (var method in methods)
        {
            if (!string.Equals(method.Name, name, StringComparison.Ordinal)) continue;
            if (!IsSupportedReturn(method, isAsync)) continue;
            if (!TryBuildInvocationInfo(method, out var info)) continue;

            return (method, info);
        }

        return (null, null);
    }

    private static bool IsSupportedReturn(IMethodShape method, bool isAsync)
    {
        if (isAsync)
        {
            if (!method.IsAsync) return false;
        }
        else if (method.IsAsync)
        {
            return false;
        }

        if (method.IsVoidLike) return true;
        return method.ReturnType.Type == typeof(int);
    }

    private static bool TryBuildInvocationInfo(IMethodShape method, out MethodInvocationInfo info)
    {
        var parameters = method.Parameters;
        var count = parameters.Count;
        var hasContext = count > 0 && parameters[index: 0].ParameterType.Type == typeof(CommandRuntimeContext);
        var hasCancellationToken = count > 0 && parameters[count - 1].ParameterType.Type == typeof(CancellationToken);

        for (var i = 0; i < count; i++)
        {
            var type = parameters[i].ParameterType.Type;
            if (type == typeof(CommandRuntimeContext) && i != 0)
            {
                info = default;
                return false;
            }

            if (type == typeof(CancellationToken) && i != count - 1)
            {
                info = default;
                return false;
            }
        }

        var kinds = new ParameterKind[count];
        for (var i = 0; i < count; i++)
        {
            if (hasContext && i == 0)
            {
                kinds[i] = ParameterKind.Context;
                continue;
            }

            if (hasCancellationToken && i == count - 1)
            {
                kinds[i] = ParameterKind.CancellationToken;
                continue;
            }

            kinds[i] = ParameterKind.Service;
        }

        info = new MethodInvocationInfo(kinds);
        return true;
    }

    private static Func<object, CommandRuntimeContext, CancellationToken, ICommandServiceResolver?, Task<int>>
        CreateInvoker(IMethodShape methodShape, MethodInvocationInfo info, Type definitionType)
    {
        var invoker =
            methodShape.Accept(
                new MethodInvokerFactory(),
                new MethodInvocationState(info.ParameterKinds, definitionType)) as Func<object, CommandRuntimeContext,
                CancellationToken, ICommandServiceResolver?, Task<int>>;
        if (invoker is null) throw new InvalidOperationException($"Unable to build handler for '{methodShape.Name}'.");

        return invoker;
    }

    private static CommandHandler CreateDefaultHelpHandler(
        BindingContext bindingContext,
        CommandRuntimeSettings settings)
    {
        Func<ParseResult, ICommandServiceResolver?, CancellationToken, Task<int>> invokeAsync = (parseResult, _, _) =>
        {
            var context = bindingContext.CreateRuntimeContext(parseResult, serviceResolver: null);
            context.ShowHelp();
            return Task.FromResult(result: 0);
        };

        Func<ParseResult, ICommandServiceResolver?, int> invoke = (parseResult, _) =>
        {
            return invokeAsync(parseResult, arg2: null, CancellationToken.None).GetAwaiter().GetResult();
        };

        return new CommandHandler(invoke, invokeAsync, isAsync: true);
    }

    private readonly record struct MethodInvocationInfo(ParameterKind[] ParameterKinds);

    private readonly record struct MethodInvocationState(ParameterKind[] ParameterKinds, Type DefinitionType);

    private enum ParameterKind
    {
        Service,
        Context,
        CancellationToken
    }

    private sealed class MethodInvokerFactory : TypeShapeVisitor
    {
        public override object? VisitMethod<TDeclaringType, TArgumentState, TResult>(
            IMethodShape<TDeclaringType, TArgumentState, TResult> methodShape,
            object? state = null)
        {
            var info = (MethodInvocationState)state!;
            var argumentStateCtor = methodShape.GetArgumentStateConstructor();
            var invoker = methodShape.GetMethodInvoker();

            var parameterSetters = methodShape.Parameters
                .Select((parameter, index) => (ArgumentStateSetter<TArgumentState>)parameter.Accept(
                    new ParameterSetterVisitor(),
                    new ParameterBindingState(info.ParameterKinds[index], info.DefinitionType))!)
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
                var binding = (ParameterBindingState)state!;
                var setter = parameterShape.GetSetter();

                ArgumentStateSetter<TArgumentState> handler = binding.Kind switch
                {
                    ParameterKind.Context => (ref argumentState, context, _, _) => setter(
                        ref argumentState,
                        (TParameterType)(object)context),
                    ParameterKind.CancellationToken => (ref argumentState, _, token, _) => setter(
                        ref argumentState,
                        (TParameterType)(object)token),
                    _ => (ref argumentState, context, cancellationToken, resolver) =>
                    {
                        var bindingContext = context.BindingContext;
                        if (bindingContext.TryResolveParentInstance(
                                context.ParseResult,
                                binding.DefinitionType,
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

        private readonly record struct ParameterBindingState(ParameterKind Kind, Type DefinitionType);
    }
}