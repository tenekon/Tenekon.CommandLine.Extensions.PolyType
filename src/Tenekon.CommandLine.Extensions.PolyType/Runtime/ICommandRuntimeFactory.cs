using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using PolyType;
using Tenekon.CommandLine.Extensions.PolyType.Constraints;
using Tenekon.CommandLine.Extensions.PolyType.Model;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;
using Tenekon.MethodOverloads;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

internal interface ICommandRuntimeFactoryMatchers
{
    [GenerateOverloads(Begin = nameof(buildOptions))]
    [OverloadGenerationOptions(BucketType = typeof(CommandRuntimeFactoryExtensions))]
    [SupplyParameterType(nameof(TConstraint), typeof(CommandObjectConstraint), Group = typeof(CommandObjectConstraint))]
    [SupplyParameterType(
        nameof(TConstraint),
        typeof(CommandFunctionConstraint),
        Group = typeof(CommandFunctionConstraint))]
    void MatcherForObject<TConstraint>(
        CommandRuntimeSettings? buildOptions,
        ICommandModelRegistry<TConstraint>? modelRegistry,
        CommandModelBuildOptions? modelBuildOptions,
        ICommandServiceResolver? serviceResolver);
}

/// <summary>
/// Creates command runtimes for the specified constraint.
/// </summary>
/// <typeparam name="TConstraint">Constraint type that selects object vs function commands.</typeparam>
[SuppressMessage("ReSharper", "TypeParameterCanBeVariant")]
[GenerateMethodOverloads(Matchers = [typeof(ICommandRuntimeFactoryMatchers)])]
public interface ICommandRuntimeFactory<TConstraint>
{
    /// <summary>
    /// Creates a runtime for the specified command type.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="commandTypeShapeProvider">The shape provider for the command type.</param>
    /// <param name="settings">Runtime settings.</param>
    /// <param name="modelRegistry">Optional model registry.</param>
    /// <param name="modelBuildOptions">Optional model build options.</param>
    /// <param name="serviceResolver">Optional service resolver.</param>
    /// <returns>The created runtime.</returns>
    CommandRuntime Create(
        Type commandType,
        ITypeShapeProvider commandTypeShapeProvider,
        CommandRuntimeSettings? settings,
        ICommandModelRegistry<TConstraint>? modelRegistry,
        CommandModelBuildOptions? modelBuildOptions,
        ICommandServiceResolver? serviceResolver);

    /// <summary>
    /// Creates a runtime for the specified command type.
    /// </summary>
    /// <typeparam name="TCommandType">The command type.</typeparam>
    /// <param name="commandTypeShapeProvider">The shape provider for the command type.</param>
    /// <param name="settings">Runtime settings.</param>
    /// <param name="modelRegistry">Optional model registry.</param>
    /// <param name="modelBuildOptions">Optional model build options.</param>
    /// <param name="serviceResolver">Optional service resolver.</param>
    /// <returns>The created runtime.</returns>
    CommandRuntime Create<TCommandType>(
        ITypeShapeProvider commandTypeShapeProvider,
        CommandRuntimeSettings? settings,
        ICommandModelRegistry<TConstraint>? modelRegistry,
        CommandModelBuildOptions? modelBuildOptions,
        ICommandServiceResolver? serviceResolver);

#if NET
    /// <summary>
    /// Creates a runtime using a shape owner type.
    /// </summary>
    /// <typeparam name="TCommandType">The command type.</typeparam>
    /// <typeparam name="TCommandTypeShapeOwner">The type that provides the shape.</typeparam>
    /// <param name="settings">Runtime settings.</param>
    /// <param name="modelRegistry">Optional model registry.</param>
    /// <param name="modelBuildOptions">Optional model build options.</param>
    /// <param name="serviceResolver">Optional service resolver.</param>
    /// <returns>The created runtime.</returns>
    CommandRuntime Create<TCommandType, TCommandTypeShapeOwner>(
        CommandRuntimeSettings? settings,
        ICommandModelRegistry<TConstraint>? modelRegistry,
        CommandModelBuildOptions? modelBuildOptions,
        ICommandServiceResolver? serviceResolver) where TCommandTypeShapeOwner : IShapeable<TCommandType>;
#endif
}

/// <summary>
/// Extension helpers for <see cref="ICommandRuntimeFactory{TConstraint}"/>.
/// </summary>
[GenerateMethodOverloads(Matchers = [typeof(ICommandRuntimeFactoryMatchers)])]
public static partial class CommandRuntimeFactoryExtensions
{
#if NET
    /// <summary>
    /// Creates a runtime using the command type as its own shape owner.
    /// </summary>
    /// <typeparam name="TConstraint">Constraint type that selects object vs function commands.</typeparam>
    /// <typeparam name="TCommandType">The command type.</typeparam>
    /// <param name="commandRuntimeFactory">The runtime factory.</param>
    /// <param name="settings">Runtime settings.</param>
    /// <param name="modelRegistry">Optional model registry.</param>
    /// <param name="modelBuildOptions">Optional model build options.</param>
    /// <param name="serviceResolver">Optional service resolver.</param>
    /// <returns>The created runtime.</returns>
    public static CommandRuntime Create<TConstraint, TCommandType>(
        this ICommandRuntimeFactory<TConstraint> commandRuntimeFactory,
        CommandRuntimeSettings? settings,
        ICommandModelRegistry<TConstraint>? modelRegistry,
        CommandModelBuildOptions? modelBuildOptions,
        ICommandServiceResolver? serviceResolver) where TCommandType : IShapeable<TCommandType>
    {
        return commandRuntimeFactory.Create<TCommandType, TCommandType>(
            settings,
            modelRegistry,
            modelBuildOptions,
            serviceResolver);
    }
#endif
}

/// <summary>
/// Creates runtimes from command models or command types.
/// </summary>
public sealed class CommandRuntimeFactory
{
    private readonly CommandRuntimeFactoryForwarder _runtimeFactoryForwarder;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandRuntimeFactory"/> class.
    /// </summary>
    public CommandRuntimeFactory()
    {
        _runtimeFactoryForwarder = new CommandRuntimeFactoryForwarder(this);
    }

    private sealed class CommandRuntimeFactoryForwarder(CommandRuntimeFactory runtimeFactory)
        : ICommandRuntimeFactory<CommandObjectConstraint>, ICommandRuntimeFactory<CommandFunctionConstraint>
    {
        CommandRuntime ICommandRuntimeFactory<CommandObjectConstraint>.Create(
            Type commandType,
            ITypeShapeProvider commandTypeShapeProvider,
            CommandRuntimeSettings? settings,
            ICommandModelRegistry<CommandObjectConstraint>? modelRegistry,
            CommandModelBuildOptions? modelBuildOptions,
            ICommandServiceResolver? serviceResolver)
        {
            modelRegistry ??= CommandModelRegistry.Shared.Object;
            var model = modelRegistry.GetOrAdd(commandType, commandTypeShapeProvider, modelBuildOptions);
            return runtimeFactory.CreateFromModel(model, settings, serviceResolver);
        }

        CommandRuntime ICommandRuntimeFactory<CommandObjectConstraint>.Create<TCommandType>(
            ITypeShapeProvider commandTypeShapeProvider,
            CommandRuntimeSettings? settings,
            ICommandModelRegistry<CommandObjectConstraint>? modelRegistry,
            CommandModelBuildOptions? modelBuildOptions,
            ICommandServiceResolver? serviceResolver)
        {
            modelRegistry ??= CommandModelRegistry.Shared.Object;
            var model = modelRegistry.GetOrAdd<TCommandType>(commandTypeShapeProvider, modelBuildOptions);
            return runtimeFactory.CreateFromModel(model, settings, serviceResolver);
        }

#if NET
        CommandRuntime ICommandRuntimeFactory<CommandObjectConstraint>.Create<TCommandType, TCommandTypeShapeOwner>(
            CommandRuntimeSettings? settings,
            ICommandModelRegistry<CommandObjectConstraint>? modelRegistry,
            CommandModelBuildOptions? modelBuildOptions,
            ICommandServiceResolver? serviceResolver)
        {
            modelRegistry ??= CommandModelRegistry.Shared.Object;
            var model = modelRegistry.GetOrAdd<TCommandType, TCommandTypeShapeOwner>(modelBuildOptions);
            return runtimeFactory.CreateFromModel(model, settings, serviceResolver);
        }
#endif

        CommandRuntime ICommandRuntimeFactory<CommandFunctionConstraint>.Create(
            Type commandType,
            ITypeShapeProvider commandTypeShapeProvider,
            CommandRuntimeSettings? settings,
            ICommandModelRegistry<CommandFunctionConstraint>? modelRegistry,
            CommandModelBuildOptions? modelBuildOptions,
            ICommandServiceResolver? serviceResolver)
        {
            modelRegistry ??= CommandModelRegistry.Shared.Function;
            var model = modelRegistry.GetOrAdd(commandType, commandTypeShapeProvider, modelBuildOptions);
            return runtimeFactory.CreateFromModel(model, settings, serviceResolver);
        }

        CommandRuntime ICommandRuntimeFactory<CommandFunctionConstraint>.Create<TCommandType>(
            ITypeShapeProvider commandTypeShapeProvider,
            CommandRuntimeSettings? settings,
            ICommandModelRegistry<CommandFunctionConstraint>? modelRegistry,
            CommandModelBuildOptions? modelBuildOptions,
            ICommandServiceResolver? serviceResolver)
        {
            modelRegistry ??= CommandModelRegistry.Shared.Function;
            var model = modelRegistry.GetOrAdd<TCommandType>(commandTypeShapeProvider, modelBuildOptions);
            return runtimeFactory.CreateFromModel(model, settings, serviceResolver);
        }

#if NET
        CommandRuntime ICommandRuntimeFactory<CommandFunctionConstraint>.Create<TCommandType, TCommandTypeShapeOwner>(
            CommandRuntimeSettings? settings,
            ICommandModelRegistry<CommandFunctionConstraint>? modelRegistry,
            CommandModelBuildOptions? modelBuildOptions,
            ICommandServiceResolver? serviceResolver)
        {
            modelRegistry ??= CommandModelRegistry.Shared.Function;
            var model = modelRegistry.GetOrAdd<TCommandType, TCommandTypeShapeOwner>(modelBuildOptions);
            return runtimeFactory.CreateFromModel(model, settings, serviceResolver);
        }
#endif
    }

    /// <summary>
    /// Gets the object command runtime factory.
    /// </summary>
    public ICommandRuntimeFactory<CommandObjectConstraint> Object => _runtimeFactoryForwarder;

    /// <summary>
    /// Gets the function command runtime factory.
    /// </summary>
    public ICommandRuntimeFactory<CommandFunctionConstraint> Function => _runtimeFactoryForwarder;

    /// <summary>
    /// Creates a runtime from an already-built command model.
    /// </summary>
    /// <param name="model">The command model.</param>
    /// <param name="settings">Runtime settings.</param>
    /// <param name="serviceResolver">Optional service resolver.</param>
    /// <returns>The created runtime.</returns>
    public CommandRuntime CreateFromModel(
        CommandModel model,
        CommandRuntimeSettings? settings,
        ICommandServiceResolver? serviceResolver)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));

        settings ??= CommandRuntimeSettings.Default;
        var (runtimeGraph, bindingContext) = CommandRuntimeBuilder.Build(model, settings);

        if (serviceResolver is not null) bindingContext.DefaultServiceResolver = serviceResolver;

        var parserConfig = new ParserConfiguration
        {
            EnablePosixBundling = settings.EnablePosixBundling
        };

        if (settings.ResponseFileTokenReplacer is not null)
            parserConfig.ResponseFileTokenReplacer = settings.ResponseFileTokenReplacer;

        return new CommandRuntime(settings, bindingContext, runtimeGraph.RootCommand, parserConfig);
    }
}
