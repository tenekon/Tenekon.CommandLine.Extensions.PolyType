using PolyType;
using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal static class CommandModelFactory
{
    public static CommandModel BuildFromObject(
        IObjectTypeShape? commandTypeShape,
        ITypeShapeProvider? commandTypeShapeProvider,
        CommandModelBuildOptions? options = null)
    {
        EnsureProvider(commandTypeShapeProvider, nameof(commandTypeShapeProvider));
        EnsureShape(commandTypeShape);

        var graph = CommandModelGraphBuilder.Build(commandTypeShape, commandTypeShapeProvider, options);
        return new CommandModel(graph);
    }

    public static CommandModel BuildFromFunction(
        IFunctionTypeShape? functionShape,
        ITypeShapeProvider? commandTypeShapeProvider,
        CommandModelBuildOptions? options = null)
    {
        EnsureProvider(commandTypeShapeProvider, nameof(commandTypeShapeProvider));
        EnsureShape(functionShape);

        var graph = CommandModelGraphBuilder.Build(functionShape, commandTypeShapeProvider, options);
        return new CommandModel(graph);
    }

    private static void EnsureProvider(ITypeShapeProvider? provider, string paramName)
    {
        if (provider is null)
            throw new ArgumentNullException(paramName, "Command type shape provider is null.");
    }

    private static void EnsureShape(object? shape)
    {
        if (shape is null)
            throw new InvalidOperationException("Command type shape is not assotiated to command type shape provider.");
    }
}