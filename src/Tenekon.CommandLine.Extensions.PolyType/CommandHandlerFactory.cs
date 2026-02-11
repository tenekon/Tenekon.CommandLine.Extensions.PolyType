using System.CommandLine;
using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType;

internal sealed class CommandHandler(
    Func<ParseResult, IServiceProvider?, int> invoke,
    Func<ParseResult, IServiceProvider?, CancellationToken, Task<int>> invokeAsync,
    bool isAsync)
{
    public Func<ParseResult, IServiceProvider?, int> Invoke { get; } = invoke;
    public Func<ParseResult, IServiceProvider?, CancellationToken, Task<int>> InvokeAsync { get; } = invokeAsync;
    public bool IsAsync { get; } = isAsync;
}

internal static class CommandHandlerFactory
{
    public static CommandHandler? TryCreateHandler(
        IObjectTypeShape shape,
        CommandLineBindingContext bindingContext,
        CommandLineSettings settings,
        CommandDescriptor rootDescriptor)
    {
        var methods = shape.Methods;
        var (asyncMethod, asyncInfo) = FindMethod(methods, "RunAsync", isAsync: true);
        var (syncMethod, syncInfo) = FindMethod(methods, "Run", isAsync: false);

        var selected = asyncMethod ?? syncMethod;
        if (selected is null)
            return CreateDefaultHelpHandler(bindingContext, settings, rootDescriptor);

        var info = asyncMethod is not null ? asyncInfo : syncInfo;
        if (info is null)
            return CreateDefaultHelpHandler(bindingContext, settings, rootDescriptor);

        var invoker = CreateInvoker(selected, info.Value);

        Func<ParseResult, IServiceProvider?, CancellationToken, Task<int>> invokeAsync = async (
            parseResult,
            serviceProvider,
            cancellationToken) =>
        {
            serviceProvider ??= bindingContext.CurrentServiceProvider ?? bindingContext.DefaultServiceProvider;
            var instance = bindingContext.Bind(parseResult, shape.Type, serviceProvider);
            var context = new CommandLineContext(bindingContext, parseResult, settings, rootDescriptor);

            if (settings.ShowHelpOnEmptyCommand && context.IsEmptyCommand())
            {
                context.ShowHelp();
                return 0;
            }

            return await invoker(instance, context, cancellationToken, serviceProvider)
                .ConfigureAwait(continueOnCapturedContext: false);
        };

        Func<ParseResult, IServiceProvider?, int> invoke = (parseResult, serviceProvider) =>
        {
            return invokeAsync(parseResult, serviceProvider, CancellationToken.None).GetAwaiter().GetResult();
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

    private static bool TryBuildInvocationInfo(
        IMethodShape method,
        out MethodInvocationInfo info)
    {
        var parameters = method.Parameters;
        var count = parameters.Count;
        var hasContext = count > 0 && parameters[index: 0].ParameterType.Type == typeof(CommandLineContext);
        var hasCancellationToken = count > 0 && parameters[count - 1].ParameterType.Type == typeof(CancellationToken);

        for (var i = 0; i < count; i++)
        {
            var type = parameters[i].ParameterType.Type;
            if (type == typeof(CommandLineContext) && i != 0)
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

    private static Func<object, CommandLineContext, CancellationToken, IServiceProvider?, Task<int>> CreateInvoker(
        IMethodShape methodShape,
        MethodInvocationInfo info)
    {
        var invoker =
            methodShape.Accept(new MethodInvokerFactory(), info) as
                Func<object, CommandLineContext, CancellationToken, IServiceProvider?, Task<int>>;
        if (invoker is null)
            throw new InvalidOperationException($"Unable to build handler for '{methodShape.Name}'.");

        return invoker;
    }

    private static CommandHandler CreateDefaultHelpHandler(
        CommandLineBindingContext bindingContext,
        CommandLineSettings settings,
        CommandDescriptor rootDescriptor)
    {
        Func<ParseResult, IServiceProvider?, CancellationToken, Task<int>> invokeAsync = (parseResult, _, _) =>
        {
            var context = new CommandLineContext(bindingContext, parseResult, settings, rootDescriptor);
            context.ShowHelp();
            return Task.FromResult(result: 0);
        };

        Func<ParseResult, IServiceProvider?, int> invoke = (parseResult, _) =>
        {
            return invokeAsync(parseResult, null, CancellationToken.None).GetAwaiter().GetResult();
        };

        return new CommandHandler(invoke, invokeAsync, isAsync: true);
    }

    private readonly record struct MethodInvocationInfo(ParameterKind[] ParameterKinds);

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
            var info = (MethodInvocationInfo)state!;
            var argumentStateCtor = methodShape.GetArgumentStateConstructor();
            var invoker = methodShape.GetMethodInvoker();

            var parameterSetters = methodShape.Parameters
                .Select((parameter, index) => (ArgumentStateSetter<TArgumentState>)parameter.Accept(
                    new ParameterSetterVisitor(),
                    info.ParameterKinds[index])!)
                .ToArray();

            return new Func<object, CommandLineContext, CancellationToken, IServiceProvider?, Task<int>>(async (
                instance,
                context,
                cancellationToken,
                serviceProvider) =>
            {
                var argumentState = argumentStateCtor();
                foreach (var setter in parameterSetters)
                    setter(ref argumentState, context, cancellationToken, serviceProvider);

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
            CommandLineContext context,
            CancellationToken cancellationToken,
            IServiceProvider? serviceProvider);

        private sealed class ParameterSetterVisitor : TypeShapeVisitor
        {
            public override object? VisitParameter<TArgumentState, TParameterType>(
                IParameterShape<TArgumentState, TParameterType> parameterShape,
                object? state = null)
            {
                var kind = (ParameterKind)state!;
                var setter = parameterShape.GetSetter();

                return kind switch
                {
                    ParameterKind.Context => new ArgumentStateSetter<TArgumentState>(
                        (ref TArgumentState argumentState,
                            CommandLineContext context,
                            CancellationToken _,
                            IServiceProvider? _) => setter(ref argumentState, (TParameterType)(object)context)),
                    ParameterKind.CancellationToken => new ArgumentStateSetter<TArgumentState>(
                        (ref TArgumentState argumentState,
                            CommandLineContext _,
                            CancellationToken token,
                            IServiceProvider? _) => setter(ref argumentState, (TParameterType)(object)token)),
                    _ => new ArgumentStateSetter<TArgumentState>(
                        (ref TArgumentState argumentState,
                            CommandLineContext _,
                            CancellationToken _,
                            IServiceProvider? provider) =>
                        {
                            var resolved = provider?.GetService(typeof(TParameterType));
                            TParameterType value;

                            if (resolved is TParameterType typed)
                            {
                                value = typed;
                            }
                            else if (resolved is null)
                            {
                                if (parameterShape.HasDefaultValue)
                                    value = parameterShape.DefaultValue!;
                                else if (!parameterShape.IsRequired)
                                    value = default!;
                                else
                                    throw new InvalidOperationException(
                                        $"Unable to resolve required parameter '{parameterShape.Name}'.");
                            }
                            else
                            {
                                value = (TParameterType)resolved;
                            }

                            setter(ref argumentState, value);
                        })
                };
            }
        }
    }
}
