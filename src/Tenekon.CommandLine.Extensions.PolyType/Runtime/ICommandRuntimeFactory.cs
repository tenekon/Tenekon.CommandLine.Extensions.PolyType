using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using PolyType;
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

[SuppressMessage("ReSharper", "TypeParameterCanBeVariant")]
[GenerateMethodOverloads(Matchers = [typeof(ICommandRuntimeFactoryMatchers)])]
public interface ICommandRuntimeFactory<TConstraint>
{
    CommandRuntime Create(
        Type commandType,
        ITypeShapeProvider commandTypeShapeProvider,
        CommandRuntimeSettings? settings,
        ICommandModelRegistry<TConstraint>? modelRegistry,
        CommandModelBuildOptions? modelBuildOptions,
        ICommandServiceResolver? serviceResolver);

    CommandRuntime Create<TCommandType>(
        ITypeShapeProvider commandTypeShapeProvider,
        CommandRuntimeSettings? settings,
        ICommandModelRegistry<TConstraint>? modelRegistry,
        CommandModelBuildOptions? modelBuildOptions,
        ICommandServiceResolver? serviceResolver);

#if NET
    CommandRuntime Create<TCommandType, TCommandTypeShapeOwner>(
        CommandRuntimeSettings? settings,
        ICommandModelRegistry<TConstraint>? modelRegistry,
        CommandModelBuildOptions? modelBuildOptions,
        ICommandServiceResolver? serviceResolver) where TCommandTypeShapeOwner : IShapeable<TCommandType>;
#endif
}

[GenerateMethodOverloads(Matchers = [typeof(ICommandRuntimeFactoryMatchers)])]
public static partial class CommandRuntimeFactoryExtensions
{
#if NET
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

public sealed class CommandRuntimeFactory
{
    private readonly CommandRuntimeFactoryForwarder _runtimeFactoryForwarder;

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

    public ICommandRuntimeFactory<CommandObjectConstraint> Object => _runtimeFactoryForwarder;
    public ICommandRuntimeFactory<CommandFunctionConstraint> Function => _runtimeFactoryForwarder;

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