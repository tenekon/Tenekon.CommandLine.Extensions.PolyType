using System.CommandLine;
using System.CommandLine.Parsing;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;

internal static class DirectiveValueHelper
{
    public static bool TryGetValue<T>(ParseResult parseResult, Directive directive, out T value)
    {
        var result = parseResult.GetResult(directive) as DirectiveResult;
        object? resolved;

        if (typeof(T) == typeof(bool))
        {
            resolved = result is not null;
        }
        else if (typeof(T) == typeof(string))
        {
            resolved = result?.Values is { Count: > 0 } ? result.Values[index: 0] : null;
        }
        else if (typeof(T) == typeof(string[]))
        {
            resolved = result?.Values?.ToArray() ?? [];
        }
        else
        {
            value = default!;
            return false;
        }

        value = (T)resolved!;
        return true;
    }
}