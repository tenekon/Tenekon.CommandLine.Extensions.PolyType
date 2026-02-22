using System.CommandLine;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Binding;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

/// <summary>
/// Represents a built command runtime that can parse and execute commands.
/// </summary>
public sealed class CommandRuntime
{
    /// <summary>
    /// Gets the default runtime factory instance.
    /// </summary>
    public static CommandRuntimeFactory Factory { get; } = new();

    private readonly CommandRuntimeSettings _settings;
    private readonly BindingContext _bindingContext;
    private readonly RootCommand _rootCommand;
    private readonly ParserConfiguration _parserConfiguration;

    internal CommandRuntime(
        CommandRuntimeSettings settings,
        BindingContext bindingContext,
        RootCommand rootCommand,
        ParserConfiguration parserConfiguration)
    {
        _settings = settings;
        _bindingContext = bindingContext;
        _rootCommand = rootCommand;
        _parserConfiguration = parserConfiguration;
    }

    /// <summary>
    /// Gets the function registry used for resolving command handlers.
    /// </summary>
    public CommandFunctionRegistry FunctionRegistry => _bindingContext.FunctionRegistry;

    /// <summary>
    /// Parses the provided arguments into a runtime result.
    /// </summary>
    /// <param name="args">Arguments to parse; when <see langword="null" />, uses the current process arguments.</param>
    /// <returns>The parse result wrapper.</returns>
    public CommandRuntimeResult Parse(string[]? args = null)
    {
        var actualArgs = args ?? Environment.GetCommandLineArgs().Skip(count: 1).ToArray();
        var parseResult = _rootCommand.Parse(actualArgs, _parserConfiguration);
        return new CommandRuntimeResult(_bindingContext, parseResult, _settings);
    }

    /// <summary>
    /// Parses and executes the command using the provided arguments.
    /// </summary>
    /// <param name="args">Arguments to parse; when <see langword="null" />, uses the current process arguments.</param>
    /// <returns>The process exit code.</returns>
    public int Run(string[]? args = null)
    {
        return Run(args, config: null);
    }

    /// <summary>
    /// Parses and executes the command asynchronously using the provided arguments.
    /// </summary>
    /// <param name="args">Arguments to parse; when <see langword="null" />, uses the current process arguments.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that resolves to the process exit code.</returns>
    public async Task<int> RunAsync(string[]? args = null, CancellationToken cancellationToken = default)
    {
        return await RunAsync(args, config: null, cancellationToken);
    }

    /// <summary>
    /// Parses and executes the command using the provided arguments and invocation options.
    /// </summary>
    /// <param name="args">Arguments to parse; when <see langword="null" />, uses the current process arguments.</param>
    /// <param name="config">Invocation options.</param>
    /// <returns>The process exit code.</returns>
    public int Run(string[]? args, CommandInvocationOptions? config)
    {
        return Parse(args).Run(config);
    }

    /// <summary>
    /// Parses and executes the command asynchronously using the provided arguments and invocation options.
    /// </summary>
    /// <param name="args">Arguments to parse; when <see langword="null" />, uses the current process arguments.</param>
    /// <param name="config">Invocation options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that resolves to the process exit code.</returns>
    public async Task<int> RunAsync(
        string[]? args,
        CommandInvocationOptions? config,
        CancellationToken cancellationToken = default)
    {
        return await Parse(args).RunAsync(config, cancellationToken);
    }
}
