using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using PolyType;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Constraints;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

/// <summary>
/// Caches command models keyed by type and shape provider.
/// </summary>
public sealed class CommandModelRegistry
{
    private static readonly CommandModelBuildOptions DefaultOptions = CommandModelBuildOptions.Default;

    private readonly CommandModelBuildOptions _defaultsOptions;
    private readonly CommandModelRegistryInternal _modelRegistryInternal;

    /// <summary>
    /// Creates a new registry with optional default build options.
    /// </summary>
    /// <param name="defaultOptions">Default options used when none are provided.</param>
    public CommandModelRegistry(CommandModelBuildOptions? defaultOptions = null)
    {
        _modelRegistryInternal = new CommandModelRegistryInternal(this);
        _defaultsOptions = defaultOptions ?? DefaultOptions;
    }

    private ImmutableDictionary<ModelKey, CacheEntry> _cache =
        ImmutableDictionary.Create<ModelKey, CacheEntry>(ModelKeyComparer.Instance);

    /// <summary>Gets a shared global registry instance.</summary>
    public static CommandModelRegistry Shared { get; } = new();

    /// <summary>Gets the object-command registry view.</summary>
    public ICommandModelRegistry<CommandObjectConstraint> Object => _modelRegistryInternal;
    /// <summary>Gets the function-command registry view.</summary>
    public ICommandModelRegistry<CommandFunctionConstraint> Function => _modelRegistryInternal;


    private CommandModel GetOrCreateFromObjectCore(
        ITypeShape? commandTypeShape,
        ITypeShapeProvider? commandTypeShapeProvider,
        CommandModelBuildOptions? options)
    {
        EnsureProvider(commandTypeShapeProvider, nameof(commandTypeShapeProvider));
        EnsureShape(commandTypeShape);

        var effectiveOptions = options ?? _defaultsOptions;
        var key = new ModelKey(commandTypeShape.Type, commandTypeShapeProvider, effectiveOptions);
        var entry = ImmutableInterlocked.GetOrAdd(
            ref _cache,
            key,
            _ => BuildEntry(commandTypeShape, commandTypeShapeProvider, effectiveOptions));

        if (entry.Error is not null) entry.Error.Throw();

        return entry.Model!;
    }

    private static void EnsureProvider(ITypeShapeProvider? provider, string paramName)
    {
        if (provider is null)
            throw new ArgumentNullException(paramName, "Command type shape provider is null.");
    }

    private static void EnsureShape(ITypeShape? shape)
    {
        if (shape is null)
            throw new InvalidOperationException("Command type shape is not assotiated to command type shape provider.");
    }

    private static CacheEntry BuildEntry(
        ITypeShape commandTypeShape,
        ITypeShapeProvider provider,
        CommandModelBuildOptions options)
    {
        try
        {
            var model = commandTypeShape switch
            {
                IObjectTypeShape objectShape => CommandModelFactory.BuildFromObject(objectShape, provider, options),
                IFunctionTypeShape functionShape => CommandModelFactory.BuildFromFunction(
                    functionShape,
                    provider,
                    options),
                _ => throw new InvalidOperationException(
                    $"Type '{commandTypeShape.Type.FullName}' is not a supported command shape.")
            };

            return CacheEntry.FromModel(model);
        }
        catch (Exception ex)
        {
            return CacheEntry.FromError(ExceptionDispatchInfo.Capture(ex));
        }
    }

    private sealed class CacheEntry(CommandModel? model, ExceptionDispatchInfo? error)
    {
        public CommandModel? Model { get; } = model;
        public ExceptionDispatchInfo? Error { get; } = error;

        public static CacheEntry FromModel(CommandModel model)
        {
            return new CacheEntry(model, error: null);
        }

        public static CacheEntry FromError(ExceptionDispatchInfo error)
        {
            return new CacheEntry(model: null, error);
        }
    }

    private readonly record struct ModelKey(
        Type CommandType,
        ITypeShapeProvider Provider,
        CommandModelBuildOptions Options);

    private sealed class ModelKeyComparer : IEqualityComparer<ModelKey>
    {
        public static readonly ModelKeyComparer Instance = new();

        public bool Equals(ModelKey x, ModelKey y)
        {
            return x.CommandType == y.CommandType && ReferenceEquals(x.Provider, y.Provider)
                && Equals(x.Options, y.Options);
        }

        public int GetHashCode(ModelKey obj)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + obj.CommandType.GetHashCode();
                hash = hash * 31 + RuntimeHelpers.GetHashCode(obj.Provider);
                hash = hash * 31 + obj.Options.GetHashCode();
                return hash;
            }
        }
    }

    private sealed class CommandModelRegistryInternal(CommandModelRegistry modelRegistry)
        : ICommandModelRegistry<CommandObjectConstraint>, ICommandModelRegistry<CommandFunctionConstraint>
    {
        CommandModel ICommandModelRegistry<CommandObjectConstraint>.GetOrAdd(
            Type commandType,
            ITypeShapeProvider commandTypeShapeProvider,
            CommandModelBuildOptions? buildOptions)
        {
            var commandTypeShape = commandTypeShapeProvider.GetTypeShape(commandType) as IObjectTypeShape;
            return modelRegistry.GetOrCreateFromObjectCore(commandTypeShape, commandTypeShapeProvider, buildOptions);
        }

        CommandModel ICommandModelRegistry<CommandObjectConstraint>.GetOrAdd<TCommandType>(
            ITypeShapeProvider commandTypeShapeProvider,
            CommandModelBuildOptions? buildOptions)
        {
            var commandTypeShape = commandTypeShapeProvider.GetTypeShape(typeof(TCommandType)) as IObjectTypeShape;
            return modelRegistry.GetOrCreateFromObjectCore(commandTypeShape, commandTypeShapeProvider, buildOptions);
        }

#if NET
        CommandModel ICommandModelRegistry<CommandObjectConstraint>.GetOrAdd<TCommandType, TCommandTypeShapeOwner>(
            CommandModelBuildOptions? buildOptions)
        {
            var typeShape = TCommandTypeShapeOwner.GetTypeShape() as IObjectTypeShape;
            var typeShapeProvider = typeShape?.Provider;
            return modelRegistry.GetOrCreateFromObjectCore(typeShape, typeShapeProvider, buildOptions);
        }
#endif

        CommandModel ICommandModelRegistry<CommandFunctionConstraint>.GetOrAdd(
            Type commandType,
            ITypeShapeProvider commandTypeShapeProvider,
            CommandModelBuildOptions? buildOptions)
        {
            var commandTypeShape = commandTypeShapeProvider.GetTypeShape(commandType) as IFunctionTypeShape;
            return modelRegistry.GetOrCreateFromObjectCore(commandTypeShape, commandTypeShapeProvider, buildOptions);
        }

        CommandModel ICommandModelRegistry<CommandFunctionConstraint>.GetOrAdd<TCommandType>(
            ITypeShapeProvider commandTypeShapeProvider,
            CommandModelBuildOptions? buildOptions)
        {
            var commandTypeShape = commandTypeShapeProvider.GetTypeShape(typeof(TCommandType)) as IFunctionTypeShape;
            return modelRegistry.GetOrCreateFromObjectCore(commandTypeShape, commandTypeShapeProvider, buildOptions);
        }

#if NET
        CommandModel ICommandModelRegistry<CommandFunctionConstraint>.GetOrAdd<TCommandType, TCommandTypeShapeOwner>(
            CommandModelBuildOptions? buildOptions)
        {
            var typeShape = TCommandTypeShapeOwner.GetTypeShape() as IFunctionTypeShape;
            var typeShapeProvider = typeShape?.Provider;
            return modelRegistry.GetOrCreateFromObjectCore(typeShape, typeShapeProvider, buildOptions);
        }
#endif
    }
}
