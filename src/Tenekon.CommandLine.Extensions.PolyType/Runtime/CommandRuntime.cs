using System.CommandLine;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Binding;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

public sealed class CommandRuntime
{
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

    public CommandFunctionRegistry FunctionRegistry => _bindingContext.FunctionRegistry;

    public CommandRuntimeResult Parse(string[]? args = null)
    {
        var actualArgs = args ?? Environment.GetCommandLineArgs().Skip(count: 1).ToArray();
        var parseResult = _rootCommand.Parse(actualArgs, _parserConfiguration);
        return new CommandRuntimeResult(_bindingContext, parseResult, _settings);
    }

    public int Run(string[]? args = null)
    {
        return Run(args, config: null);
    }

    public async Task<int> RunAsync(string[]? args = null, CancellationToken cancellationToken = default)
    {
        return await RunAsync(args, config: null, cancellationToken);
    }

    public int Run(string[]? args, CommandInvocationOptions? config)
    {
        return Parse(args).Run(config);
    }

    public async Task<int> RunAsync(
        string[]? args,
        CommandInvocationOptions? config,
        CancellationToken cancellationToken = default)
    {
        return await Parse(args).RunAsync(config, cancellationToken);
    }
}