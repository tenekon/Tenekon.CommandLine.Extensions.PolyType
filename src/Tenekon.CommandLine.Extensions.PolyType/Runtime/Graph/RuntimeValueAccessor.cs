using System.CommandLine;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Graph;

internal readonly record struct RuntimeValueAccessor(string DisplayName, Func<object?, ParseResult, object?> Getter);