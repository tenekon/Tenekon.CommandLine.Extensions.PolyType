using System.CommandLine;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal sealed record ArgumentMemberBuildResult(Symbol Symbol, Action<object, ParseResult> Binder);