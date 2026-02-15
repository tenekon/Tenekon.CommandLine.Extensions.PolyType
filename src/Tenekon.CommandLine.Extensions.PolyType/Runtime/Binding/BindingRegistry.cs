using System.CommandLine;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Binding;

internal sealed class BindingRegistry
{
    public Dictionary<Type, Func<BindingContext, ParseResult, ICommandServiceResolver?, CancellationToken, object>>
        CreatorMap { get; } = new();

    public Dictionary<BinderKey, Action<object, ParseResult>> BinderMap { get; } = new();
}