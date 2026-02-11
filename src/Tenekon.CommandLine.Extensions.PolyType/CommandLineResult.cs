using System.CommandLine;

namespace Tenekon.CommandLine.Extensions.PolyType;

public sealed class CommandLineResult
{
    private readonly CommandLineBindingContext _bindingContext;
    private readonly CommandLineSettings _settings;

    internal CommandLineResult(
        CommandLineBindingContext bindingContext,
        ParseResult parseResult,
        CommandLineSettings settings)
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
        return _bindingContext.Bind(ParseResult, definitionType, returnEmpty);
    }

    public bool TryGetCalledType(out Type? value)
    {
        return _bindingContext.TryGetCalledType(ParseResult, out value);
    }

    public bool TryBindCalled(out object? value)
    {
        value = null;
        if (!_bindingContext.TryGetCalledType(ParseResult, out var type) || type is null)
            return false;

        value = _bindingContext.Bind(ParseResult, type);
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

    public int Run(CommandInvocationConfiguration? config)
    {
        var priorProvider = _bindingContext.CurrentServiceProvider;
        _bindingContext.CurrentServiceProvider = config?.ServiceProvider;
        try
        {
            return ParseResult.Invoke(CreateInvocationConfiguration());
        }
        finally
        {
            _bindingContext.CurrentServiceProvider = priorProvider;
        }
    }

    public Task<int> RunAsync(CommandInvocationConfiguration? config, CancellationToken cancellationToken = default)
    {
        var priorProvider = _bindingContext.CurrentServiceProvider;
        _bindingContext.CurrentServiceProvider = config?.ServiceProvider;
        try
        {
            return ParseResult.InvokeAsync(CreateInvocationConfiguration(), cancellationToken);
        }
        finally
        {
            _bindingContext.CurrentServiceProvider = priorProvider;
        }
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
