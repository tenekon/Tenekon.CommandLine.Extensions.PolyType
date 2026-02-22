using System.Diagnostics.CodeAnalysis;
using PolyType;
using Tenekon.CommandLine.Extensions.PolyType.Constraints;
using Tenekon.MethodOverloads;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal interface ICommandRegistryOverloadGenerationMatchers
{
    [GenerateOverloads(nameof(buildOptions))]
    [OverloadGenerationOptions(BucketType = typeof(CommandModelRegistryExtensions))]
    [SupplyParameterType(nameof(TConstraint), typeof(CommandObjectConstraint), Group = typeof(CommandObjectConstraint))]
    [SupplyParameterType(
        nameof(TConstraint),
        typeof(CommandFunctionConstraint),
        Group = typeof(CommandFunctionConstraint))]
    void Matcher<TConstraint>(CommandModelBuildOptions? buildOptions);
}

/// <summary>
/// Provides cached command models for a given constraint.
/// </summary>
/// <typeparam name="TConstraint">Constraint type that selects object vs function commands.</typeparam>
[SuppressMessage("ReSharper", "TypeParameterCanBeVariant")]
[GenerateMethodOverloads(Matchers = [typeof(ICommandRegistryOverloadGenerationMatchers)])]
public interface ICommandModelRegistry<TConstraint>
{
    /// <summary>
    /// Gets or creates a model for the specified command type.
    /// </summary>
    /// <param name="commandType">Command type to build a model for.</param>
    /// <param name="commandTypeShapeProvider">Shape provider for the command type.</param>
    /// <param name="buildOptions">Optional build options.</param>
    /// <returns>The cached or newly built command model.</returns>
    CommandModel GetOrAdd(
        Type commandType,
        ITypeShapeProvider commandTypeShapeProvider,
        CommandModelBuildOptions? buildOptions);

    /// <summary>
    /// Gets or creates a model for the specified command type.
    /// </summary>
    /// <typeparam name="TCommandType">Command type to build a model for.</typeparam>
    /// <param name="commandTypeShapeProvider">Shape provider for the command type.</param>
    /// <param name="buildOptions">Optional build options.</param>
    /// <returns>The cached or newly built command model.</returns>
    CommandModel GetOrAdd<TCommandType>(
        ITypeShapeProvider commandTypeShapeProvider,
        CommandModelBuildOptions? buildOptions);

#if NET
    /// <summary>
    /// Gets or creates a model using a shape provider from a shape owner type.
    /// </summary>
    /// <typeparam name="TCommandType">Command type to build a model for.</typeparam>
    /// <typeparam name="TCommandTypeShapeOwner">Type that provides the shape for the command type.</typeparam>
    /// <param name="buildOptions">Optional build options.</param>
    /// <returns>The cached or newly built command model.</returns>
    CommandModel GetOrAdd<TCommandType, TCommandTypeShapeOwner>(CommandModelBuildOptions? buildOptions)
        where TCommandTypeShapeOwner : IShapeable<TCommandType>;
#endif
}

/// <summary>
/// Extension helpers for <see cref="ICommandModelRegistry{TConstraint}"/>.
/// </summary>
[GenerateMethodOverloads(Matchers = [typeof(ICommandRegistryOverloadGenerationMatchers)])]
public static partial class CommandModelRegistryExtensions
{
#if NET
    /// <summary>
    /// Gets or creates a model using the command type as its own shape owner.
    /// </summary>
    /// <typeparam name="TConstraint">Constraint type that selects object vs function commands.</typeparam>
    /// <typeparam name="TCommandType">Command type to build a model for.</typeparam>
    /// <param name="commandModelRegistry">Registry instance.</param>
    /// <param name="buildOptions">Optional build options.</param>
    /// <returns>The cached or newly built command model.</returns>
    [GenerateOverloads(Matchers = [typeof(ICommandRegistryOverloadGenerationMatchers)])]
    public static CommandModel GetOrAdd<TConstraint, TCommandType>(
        this ICommandModelRegistry<TConstraint> commandModelRegistry,
        CommandModelBuildOptions? buildOptions) where TCommandType : IShapeable<TCommandType>
    {
        return commandModelRegistry.GetOrAdd<TCommandType, TCommandType>(buildOptions);
    }
#endif
}
