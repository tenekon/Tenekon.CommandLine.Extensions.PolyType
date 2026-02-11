using System.CommandLine;
using PolyType;
using PolyType.Abstractions;
using Tenekon.MethodOverloads.SourceGenerator;

namespace Tenekon.CommandLine.Extensions.PolyType;

public sealed class CommandLineApp
{
    private readonly CommandLineSettings _settings;
    private readonly CommandLineBindingContext _bindingContext;
    private readonly RootCommand _rootCommand;
    private readonly ParserConfiguration _parserConfiguration;

    private CommandLineApp(
        CommandLineSettings settings,
        CommandLineBindingContext bindingContext,
        RootCommand rootCommand,
        ParserConfiguration parserConfiguration)
    {
        _settings = settings;
        _bindingContext = bindingContext;
        _rootCommand = rootCommand;
        _parserConfiguration = parserConfiguration;
    }

#if NET
    [GenerateOverloads]
    public static CommandLineApp CreateFromType<TCommand>(
        CommandLineSettings? settings,
        IServiceProvider? serviceProvider,
        ITypeShapeProvider? commandTypeShapeProvider) where TCommand : IShapeable<TCommand>
    {
        var commandTypeShape = TCommand.GetTypeShape() as IObjectTypeShape;
        var commandTypeShapeProvider2 = commandTypeShapeProvider ?? commandTypeShape?.Provider;
        return CreateCore(commandTypeShape, commandTypeShapeProvider2, settings, serviceProvider);
    }
#endif

    [GenerateOverloads(Begin = nameof(settings))]
    public static CommandLineApp CreateFromProvider(
        Type commandType,
        ITypeShapeProvider commandTypeShapeProvider,
        CommandLineSettings? settings,
        IServiceProvider? serviceProvider)
    {
        var commandTypeShape = commandTypeShapeProvider.GetTypeShape(commandType) as IObjectTypeShape;
        return CreateCore(commandTypeShape, commandTypeShapeProvider, settings, serviceProvider);
    }

    [GenerateOverloads(Begin = nameof(settings))]
    public static CommandLineApp CreateFromProvider<TCommand>(
        ITypeShapeProvider commandTypeShapeProvider,
        CommandLineSettings? settings,
        IServiceProvider? serviceProvider)
    {
        var commandTypeShape = commandTypeShapeProvider.GetTypeShape(typeof(TCommand)) as IObjectTypeShape;
        return CreateCore(commandTypeShape, commandTypeShapeProvider, settings, serviceProvider);
    }

    private static CommandLineApp CreateCore(
        IObjectTypeShape? commandTypeShape,
        ITypeShapeProvider? commandTypeShapeProvider,
        CommandLineSettings? settings,
        IServiceProvider? serviceProvider)
    {
        if (commandTypeShapeProvider is null)
        {
            throw new ArgumentNullException(nameof(commandTypeShapeProvider), "Command type shape provider is null.");
        }

        if (commandTypeShape is null)
        {
            throw new InvalidOperationException("Command type shape is not assotiated to command type shape provider.");
        }

        settings ??= CommandLineSettings.Default;
        var graph = CommandGraphBuilder.Build(commandTypeShape, commandTypeShapeProvider, settings, serviceProvider);
        var parserConfig = new ParserConfiguration
        {
            EnablePosixBundling = settings.EnablePosixBundling
        };

        if (settings.ResponseFileTokenReplacer is not null)
            parserConfig.ResponseFileTokenReplacer = settings.ResponseFileTokenReplacer;

        return new CommandLineApp(settings, graph.BindingContext, graph.RootCommand, parserConfig);
    }

    public CommandLineResult Parse(string[]? args = null)
    {
        var actualArgs = args ?? Environment.GetCommandLineArgs().Skip(count: 1).ToArray();
        var parseResult = _rootCommand.Parse(actualArgs, _parserConfiguration);
        return new CommandLineResult(_bindingContext, parseResult, _settings);
    }

    public int Run(string[]? args = null)
    {
        return Run(args, config: null);
    }

    public async Task<int> RunAsync(string[]? args = null, CancellationToken cancellationToken = default)
    {
        return await RunAsync(args, config: null, cancellationToken);
    }

    public int Run(string[]? args, CommandInvocationConfiguration? config)
    {
        return Parse(args).Run(config);
    }

    public async Task<int> RunAsync(
        string[]? args,
        CommandInvocationConfiguration? config,
        CancellationToken cancellationToken = default)
    {
        return await Parse(args).RunAsync(config, cancellationToken);
    }
}

internal readonly record struct CommandGraph(
    RootCommand RootCommand,
    CommandDescriptor RootDescriptor,
    CommandLineBindingContext BindingContext);

internal static class CommandGraphBuilder
{
    public static CommandGraph Build(
        IObjectTypeShape rootShape,
        ITypeShapeProvider provider,
        CommandLineSettings settings,
        IServiceProvider? serviceProvider)
    {
        var descriptors = new Dictionary<Type, CommandDescriptor>();
        var rootDescriptor = EnsureDescriptor(rootShape, provider, descriptors);
        rootDescriptor = rootDescriptor.GetRoot();
        rootDescriptor.LinkRelationships(provider, descriptors);
        rootDescriptor.ValidateNoCycles();

        var bindingContext = new CommandLineBindingContext(descriptors);
        bindingContext.DefaultServiceProvider = serviceProvider;
        rootDescriptor.BuildCommands(bindingContext, settings, serviceProvider, parentNamer: null, rootDescriptor);

        if (rootDescriptor.Command is not RootCommand rootCommand)
            throw new InvalidOperationException("Root command is not a RootCommand.");

        return new CommandGraph(rootCommand, rootDescriptor, bindingContext);
    }

    private static CommandDescriptor EnsureDescriptor(
        IObjectTypeShape shape,
        ITypeShapeProvider provider,
        Dictionary<Type, CommandDescriptor> descriptors)
    {
        var type = shape.Type;
        if (descriptors.TryGetValue(type, out var existing))
            return existing;

        var spec = shape.AttributeProvider.GetCustomAttribute<CommandSpecAttribute>();
        if (spec is null)
            throw new InvalidOperationException($"Type '{type.FullName}' is missing [CommandSpec].");

        var descriptor = new CommandDescriptor(type, shape, spec);
        descriptors[type] = descriptor;

        if (spec.Parent is not null)
        {
            var parentShape = provider.GetTypeShape(spec.Parent) as IObjectTypeShape;
            if (parentShape is not null)
                EnsureDescriptor(parentShape, provider, descriptors);
        }

        if (spec.Children is not null)
            foreach (var childType in spec.Children)
            {
                if (childType is null) continue;
                var childShape = provider.GetTypeShape(childType) as IObjectTypeShape;
                if (childShape is not null)
                    EnsureDescriptor(childShape, provider, descriptors);
            }

        foreach (var nested in type.GetNestedTypes())
        {
            var nestedShape = provider.GetTypeShape(nested) as IObjectTypeShape;
            if (nestedShape is null) continue;

            var nestedSpec = nestedShape.AttributeProvider.GetCustomAttribute<CommandSpecAttribute>();
            if (nestedSpec is null) continue;

            EnsureDescriptor(nestedShape, provider, descriptors);
        }

        return descriptor;
    }
}