namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;

/// <summary>
/// Configures service and function resolution during invocation.
/// </summary>
public sealed class CommandInvocationOptions
{
    /// <summary>
    /// Gets or sets the service resolver used during invocation.
    /// </summary>
    public ICommandServiceResolver? ServiceResolver { get; set; }

    /// <summary>
    /// Gets or sets the function resolver used during invocation.
    /// </summary>
    public ICommandFunctionResolver? FunctionResolver { get; set; }
}
