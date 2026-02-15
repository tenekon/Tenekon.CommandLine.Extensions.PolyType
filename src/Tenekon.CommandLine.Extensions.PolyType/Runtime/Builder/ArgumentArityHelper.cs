using System.CommandLine;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal static class ArgumentArityHelper
{
    public static ArgumentArity Map(Spec.ArgumentArity arity)
    {
        return arity switch
        {
            Spec.ArgumentArity.Zero => ArgumentArity.Zero,
            Spec.ArgumentArity.ZeroOrOne => ArgumentArity.ZeroOrOne,
            Spec.ArgumentArity.ExactlyOne => ArgumentArity.ExactlyOne,
            Spec.ArgumentArity.ZeroOrMore => ArgumentArity.ZeroOrMore,
            Spec.ArgumentArity.OneOrMore => ArgumentArity.OneOrMore,
            _ => ArgumentArity.ZeroOrMore
        };
    }
}