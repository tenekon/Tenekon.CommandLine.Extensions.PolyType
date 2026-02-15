namespace Tenekon.CommandLine.Extensions.PolyType.Model;

public sealed class CommandModel
{
    internal CommandModel(CommandModelGraph graph)
    {
        Graph = graph;
    }

    internal CommandModelGraph Graph { get; }

    public Type DefinitionType =>
        Graph.RootNode.CommandType ?? throw new InvalidOperationException("Root command type is not available.");
}