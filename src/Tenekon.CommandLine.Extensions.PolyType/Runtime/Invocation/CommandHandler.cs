using System.CommandLine;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;

internal sealed class CommandHandler(
    Func<ParseResult, ICommandServiceResolver?, int> invoke,
    Func<ParseResult, ICommandServiceResolver?, CancellationToken, Task<int>> invokeAsync,
    bool isAsync)
{
    public Func<ParseResult, ICommandServiceResolver?, int> Invoke { get; } = invoke;
    public Func<ParseResult, ICommandServiceResolver?, CancellationToken, Task<int>> InvokeAsync { get; } = invokeAsync;
    public bool IsAsync { get; } = isAsync;
}