using System.CommandLine;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Model;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal sealed class DirectiveMemberBuilder(
    IPropertyShape propertyShape,
    DirectiveSpecAttribute spec,
    CommandNamingPolicy namer)
{
    public DirectiveBuildResult? Build()
    {
        return (DirectiveBuildResult?)propertyShape.Accept(new BuilderVisitor(spec, namer));
    }

    private sealed class BuilderVisitor(DirectiveSpecAttribute spec, CommandNamingPolicy namer) : TypeShapeVisitor
    {
        public override object? VisitProperty<TDeclaringType, TPropertyType>(
            IPropertyShape<TDeclaringType, TPropertyType> propertyShape,
            object? state = null)
        {
            var name = namer.GetDirectiveName(propertyShape.Name, spec.Name);
            var directive = new Directive(name);

            Action<object, ParseResult> binder = (instance, parseResult) =>
            {
                var typedInstance = (TDeclaringType)instance;
                var setter = propertyShape.GetSetter();
                if (!DirectiveValueHelper.TryGetValue(parseResult, directive, out TPropertyType value)) return;
                setter(ref typedInstance, value);
            };

            return new DirectiveBuildResult(directive, binder);
        }
    }
}