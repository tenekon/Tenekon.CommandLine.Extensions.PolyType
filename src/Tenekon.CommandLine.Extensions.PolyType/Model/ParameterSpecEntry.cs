using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal readonly record struct ParameterSpecEntry(
    IParameterShape Parameter,
    OptionSpecAttribute? Option,
    ArgumentSpecAttribute? Argument,
    DirectiveSpecAttribute? Directive);