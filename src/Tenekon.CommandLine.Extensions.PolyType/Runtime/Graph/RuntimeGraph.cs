using System.CommandLine;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Graph;

internal readonly record struct RuntimeGraph(RootCommand RootCommand, RuntimeNode RootNode);