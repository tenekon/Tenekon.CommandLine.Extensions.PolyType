using System.CommandLine;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Binding;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

public sealed class CommandRuntimeResult
{
    private readonly BindingContext _bindingContext;
    private readonly CommandRuntimeSettings _settings;

    internal CommandRuntimeResult(
        BindingContext bindingContext,
        ParseResult parseResult,
        CommandRuntimeSettings settings)
    {
        _bindingContext = bindingContext;
        ParseResult = parseResult;
        _settings = settings;
    }

    public ParseResult ParseResult { get; }

    public TDefinition Bind<TDefinition>(bool returnEmpty = false)
    {
        return _bindingContext.Bind<TDefinition>(ParseResult, returnEmpty);
    }

    public object Bind(Type definitionType, bool returnEmpty = false)
    {
        return _bindingContext.Bind(ParseResult, definitionType, returnEmpty, cancellationToken: default);
    }

    public bool TryGetCalledType(out Type? value)
    {
        return _bindingContext.TryGetCalledType(ParseResult, out value);
    }

    public bool TryBindCalled(out object? value)
    {
        value = null;
        if (!_bindingContext.TryGetCalledType(ParseResult, out var type) || type is null) return false;

        value = _bindingContext.Bind(ParseResult, type, returnEmpty: false, cancellationToken: default);
        return true;
    }

    public object BindCalled()
    {
        return _bindingContext.BindCalled(ParseResult);
    }

    public object[] BindAll()
    {
        return _bindingContext.BindAll(ParseResult);
    }

    public bool TryGetBinder(Type commandType, Type targetType, out Action<object, ParseResult>? binder)
    {
        return _bindingContext.BinderMap.TryGetValue(new BinderKey(commandType, targetType), out binder);
    }

    public void Bind<TCommand, TTarget>(TTarget instance)
    {
        if (!TryGetBinder(typeof(TCommand), typeof(TTarget), out var binder) || binder is null)
            throw new InvalidOperationException(
                $"Binder is not found for command '{typeof(TCommand).FullName}' and target '{typeof(TTarget).FullName}'.");

        binder(instance!, ParseResult);
    }

    public bool IsCalled<TDefinition>()
    {
        return _bindingContext.IsCalled<TDefinition>(ParseResult);
    }

    public bool IsCalled(Type definitionType)
    {
        return _bindingContext.IsCalled(ParseResult, definitionType);
    }

    public bool Contains<TDefinition>()
    {
        return _bindingContext.Contains<TDefinition>(ParseResult);
    }

    public bool Contains(Type definitionType)
    {
        return _bindingContext.Contains(ParseResult, definitionType);
    }

    public int Run()
    {
        return Run(config: null);
    }

    public Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        return RunAsync(config: null, cancellationToken);
    }

    public int Run(CommandInvocationOptions? config)
    {
        var priorResolver = _bindingContext.CurrentServiceResolver;
        _bindingContext.CurrentServiceResolver = CreateInvocationServiceResolver(config);
        try
        {
            return ParseResult.Invoke(CreateInvocationConfiguration());
        }
        finally
        {
            _bindingContext.CurrentServiceResolver = priorResolver;
        }
    }

    public Task<int> RunAsync(CommandInvocationOptions? config, CancellationToken cancellationToken = default)
    {
        var priorResolver = _bindingContext.CurrentServiceResolver;
        _bindingContext.CurrentServiceResolver = CreateInvocationServiceResolver(config);
        try
        {
            return ParseResult.InvokeAsync(CreateInvocationConfiguration(), cancellationToken);
        }
        finally
        {
            _bindingContext.CurrentServiceResolver = priorResolver;
        }
    }

    private ICommandServiceResolver? CreateInvocationServiceResolver(CommandInvocationOptions? config)
    {
        var serviceResolver = config?.ServiceResolver ?? _bindingContext.DefaultServiceResolver;
        var functionResolver = _bindingContext.CreateFunctionResolver(serviceResolver, config?.FunctionResolver);
        if (functionResolver is null) return config?.ServiceResolver;

        return new CommandInvocationServiceResolver(serviceResolver, functionResolver);
    }

    private InvocationConfiguration CreateInvocationConfiguration()
    {
        return new InvocationConfiguration
        {
            EnableDefaultExceptionHandler = _settings.EnableDefaultExceptionHandler,
            Output = _settings.Output,
            Error = _settings.Error
        };
    }
}