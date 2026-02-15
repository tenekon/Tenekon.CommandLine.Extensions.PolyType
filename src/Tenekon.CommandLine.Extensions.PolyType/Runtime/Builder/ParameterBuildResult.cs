using System.CommandLine;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Graph;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal readonly record struct ParameterBuildResult(object Symbol, RuntimeValueAccessor Accessor);

internal readonly record struct DirectiveParameterBuildResult(Directive Directive, RuntimeValueAccessor Accessor);