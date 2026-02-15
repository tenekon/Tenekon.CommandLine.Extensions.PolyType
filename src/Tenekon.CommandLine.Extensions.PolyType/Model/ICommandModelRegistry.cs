using System.Diagnostics.CodeAnalysis;
using PolyType;
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

[SuppressMessage("ReSharper", "TypeParameterCanBeVariant")]
[GenerateMethodOverloads(Matchers = [typeof(ICommandRegistryOverloadGenerationMatchers)])]
public interface ICommandModelRegistry<TConstraint>
{
    CommandModel GetOrAdd(
        Type commandType,
        ITypeShapeProvider commandTypeShapeProvider,
        CommandModelBuildOptions? buildOptions);

    CommandModel GetOrAdd<TCommandType>(
        ITypeShapeProvider commandTypeShapeProvider,
        CommandModelBuildOptions? buildOptions);

#if NET
    CommandModel GetOrAdd<TCommandType, TCommandTypeShapeOwner>(CommandModelBuildOptions? buildOptions)
        where TCommandTypeShapeOwner : IShapeable<TCommandType>;
#endif
}

[GenerateMethodOverloads(Matchers = [typeof(ICommandRegistryOverloadGenerationMatchers)])]
public static partial class CommandModelRegistryExtensions
{
#if NET
    [GenerateOverloads(Matchers = [typeof(ICommandRegistryOverloadGenerationMatchers)])]
    public static CommandModel GetOrAdd<TConstraint, TCommandType>(
        this ICommandModelRegistry<TConstraint> commandModelRegistry,
        CommandModelBuildOptions? buildOptions) where TCommandType : IShapeable<TCommandType>
    {
        return commandModelRegistry.GetOrAdd<TCommandType, TCommandType>(buildOptions);
    }
#endif
}