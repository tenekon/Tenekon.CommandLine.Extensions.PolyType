namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;

public sealed class CommandInvocationOptions
{
    public ICommandServiceResolver? ServiceResolver { get; set; }
    public ICommandFunctionResolver? FunctionResolver { get; set; }
}