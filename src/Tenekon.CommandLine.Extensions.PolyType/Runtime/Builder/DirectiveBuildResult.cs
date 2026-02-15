using System.CommandLine;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal sealed record DirectiveBuildResult(Directive Directive, Action<object, ParseResult> Binder);