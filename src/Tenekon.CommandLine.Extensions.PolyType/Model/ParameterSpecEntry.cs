using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal readonly record struct ParameterSpecEntry(
    IParameterShape Parameter,
    OptionSpecModel? Option,
    ArgumentSpecModel? Argument,
    DirectiveSpecModel? Directive);