using System.CommandLine;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Binding;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

/// <summary>
/// Provides binding and invocation operations for a parsed command line.
/// </summary>
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

    /// <summary>
    /// Gets the underlying parse result.
    /// </summary>
    public ParseResult ParseResult { get; }

    /// <summary>
    /// Binds the parsed command line to a new instance of the specified definition type.
    /// </summary>
    /// <typeparam name="TDefinition">The command definition type.</typeparam>
    /// <param name="returnEmpty">Whether to return an empty instance when no command was called.</param>
    /// <returns>The bound command instance.</returns>
    public TDefinition Bind<TDefinition>(bool returnEmpty = false)
    {
        return _bindingContext.Bind<TDefinition>(ParseResult, returnEmpty);
    }

    /// <summary>
    /// Binds the parsed command line to a new instance of the specified definition type.
    /// </summary>
    /// <param name="definitionType">The command definition type.</param>
    /// <param name="returnEmpty">Whether to return an empty instance when no command was called.</param>
    /// <returns>The bound command instance.</returns>
    public object Bind(Type definitionType, bool returnEmpty = false)
    {
        return _bindingContext.Bind(ParseResult, definitionType, returnEmpty, cancellationToken: default);
    }

    /// <summary>
    /// Attempts to get the type for the command that was invoked.
    /// </summary>
    /// <param name="value">The called command type, when available.</param>
    /// <returns><see langword="true" /> if a command was invoked; otherwise <see langword="false" />.</returns>
    public bool TryGetCalledType(out Type? value)
    {
        return _bindingContext.TryGetCalledType(ParseResult, out value);
    }

    /// <summary>
    /// Attempts to bind the invoked command.
    /// </summary>
    /// <param name="value">The bound command instance, when available.</param>
    /// <returns><see langword="true" /> if a command was invoked and bound; otherwise <see langword="false" />.</returns>
    public bool TryBindCalled(out object? value)
    {
        value = null;
        if (!_bindingContext.TryGetCalledType(ParseResult, out var type) || type is null) return false;

        value = _bindingContext.Bind(ParseResult, type, returnEmpty: false, cancellationToken: default);
        return true;
    }

    /// <summary>
    /// Binds the invoked command.
    /// </summary>
    /// <returns>The bound command instance.</returns>
    public object BindCalled()
    {
        return _bindingContext.BindCalled(ParseResult);
    }

    /// <summary>
    /// Binds all commands in the parsed graph.
    /// </summary>
    /// <returns>An array of bound command instances.</returns>
    public object[] BindAll()
    {
        return _bindingContext.BindAll(ParseResult);
    }

    /// <summary>
    /// Attempts to get a binder for a command type and target type.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="binder">The binder action, when available.</param>
    /// <returns><see langword="true" /> if a binder is available; otherwise <see langword="false" />.</returns>
    public bool TryGetBinder(Type commandType, Type targetType, out Action<object, ParseResult>? binder)
    {
        return _bindingContext.BinderMap.TryGetValue(new BinderKey(commandType, targetType), out binder);
    }

    /// <summary>
    /// Binds values for a command into an existing target instance.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="instance">The instance to populate.</param>
    public void Bind<TCommand, TTarget>(TTarget instance)
    {
        if (!TryGetBinder(typeof(TCommand), typeof(TTarget), out var binder) || binder is null)
            throw new InvalidOperationException(
                $"Binder is not found for command '{typeof(TCommand).FullName}' and target '{typeof(TTarget).FullName}'.");

        binder(instance!, ParseResult);
    }

    /// <summary>
    /// Determines whether the specified command was invoked.
    /// </summary>
    /// <typeparam name="TDefinition">The command type.</typeparam>
    /// <returns><see langword="true" /> if invoked; otherwise <see langword="false" />.</returns>
    public bool IsCalled<TDefinition>()
    {
        return _bindingContext.IsCalled<TDefinition>(ParseResult);
    }

    /// <summary>
    /// Determines whether the specified command was invoked.
    /// </summary>
    /// <param name="definitionType">The command type.</param>
    /// <returns><see langword="true" /> if invoked; otherwise <see langword="false" />.</returns>
    public bool IsCalled(Type definitionType)
    {
        return _bindingContext.IsCalled(ParseResult, definitionType);
    }

    /// <summary>
    /// Determines whether the parsed graph contains the specified command.
    /// </summary>
    /// <typeparam name="TDefinition">The command type.</typeparam>
    /// <returns><see langword="true" /> if present; otherwise <see langword="false" />.</returns>
    public bool Contains<TDefinition>()
    {
        return _bindingContext.Contains<TDefinition>(ParseResult);
    }

    /// <summary>
    /// Determines whether the parsed graph contains the specified command.
    /// </summary>
    /// <param name="definitionType">The command type.</param>
    /// <returns><see langword="true" /> if present; otherwise <see langword="false" />.</returns>
    public bool Contains(Type definitionType)
    {
        return _bindingContext.Contains(ParseResult, definitionType);
    }

    /// <summary>
    /// Executes the parsed command using default invocation options.
    /// </summary>
    /// <returns>The process exit code.</returns>
    public int Run()
    {
        return Run(config: null);
    }

    /// <summary>
    /// Executes the parsed command asynchronously using default invocation options.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that resolves to the process exit code.</returns>
    public Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        return RunAsync(config: null, cancellationToken);
    }

    /// <summary>
    /// Executes the parsed command using the specified invocation options.
    /// </summary>
    /// <param name="config">Invocation options.</param>
    /// <returns>The process exit code.</returns>
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

    /// <summary>
    /// Executes the parsed command asynchronously using the specified invocation options.
    /// </summary>
    /// <param name="config">Invocation options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that resolves to the process exit code.</returns>
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
