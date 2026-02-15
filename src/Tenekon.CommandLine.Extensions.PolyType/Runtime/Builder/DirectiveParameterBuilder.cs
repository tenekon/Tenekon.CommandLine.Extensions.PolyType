using System.CommandLine;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Model;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Graph;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal sealed class DirectiveParameterBuilder(
    IParameterShape parameterShape,
    DirectiveSpecAttribute spec,
    CommandNamingPolicy namer)
{
    public DirectiveParameterBuildResult? Build()
    {
        return (DirectiveParameterBuildResult?)parameterShape.Accept(new BuilderVisitor(spec, namer));
    }

    private sealed class BuilderVisitor(DirectiveSpecAttribute spec, CommandNamingPolicy namer) : TypeShapeVisitor
    {
        public override object? VisitParameter<TArgumentState, TParameterType>(
            IParameterShape<TArgumentState, TParameterType> parameterShape,
            object? state = null)
        {
            var name = namer.GetDirectiveName(parameterShape.Name, spec.Name);
            var directive = new Directive(name);

            var accessor = new RuntimeValueAccessor(
                parameterShape.Name,
                (_, parseResult) =>
                {
                    return DirectiveValueHelper.TryGetValue(parseResult, directive, out TParameterType value)
                        ? value
                        : null;
                });

            return new DirectiveParameterBuildResult(directive, accessor);
        }
    }
}