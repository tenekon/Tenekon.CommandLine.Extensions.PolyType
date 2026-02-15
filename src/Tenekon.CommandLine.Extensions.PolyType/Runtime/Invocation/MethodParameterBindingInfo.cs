namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;

internal enum MethodParameterBindingKind
{
    Service,
    Context,
    CancellationToken,
    Option,
    Argument,
    Directive
}

internal readonly record struct MethodParameterBindingInfo(MethodParameterBindingKind Kind, object? Symbol);