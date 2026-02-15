using System.CommandLine;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Model;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal sealed class OptionMemberBuilder(
    IPropertyShape propertyShape,
    IPropertyShape valueProperty,
    OptionSpecAttribute spec,
    CommandNamingPolicy namer,
    IFileSystem fileSystem)
{
    public ArgumentMemberBuildResult? Build()
    {
        return (ArgumentMemberBuildResult?)propertyShape.Accept(
            new BuilderVisitor(spec, namer, valueProperty, fileSystem));
    }

    private sealed class BuilderVisitor(
        OptionSpecAttribute spec,
        CommandNamingPolicy namer,
        IPropertyShape valueProperty,
        IFileSystem fileSystem) : TypeShapeVisitor
    {
        public override object? VisitProperty<TDeclaringType, TPropertyType>(
            IPropertyShape<TDeclaringType, TPropertyType> propertyShape,
            object? state = null)
        {
            var name = namer.GetOptionName(propertyShape.Name, spec.Name);
            var required = RequiredHelper.IsRequired(valueProperty, spec);
            var option = SymbolBuildHelper.CreateOption<TPropertyType>(name, spec, namer, required, fileSystem);

            var setter = propertyShape.GetSetter();

            return new ArgumentMemberBuildResult(option, Binder);

            void Binder(object instance, ParseResult parseResult)
            {
                var typedInstance = (TDeclaringType)instance;
                var value = parseResult.GetValue(option);
                if (value is null && !typeof(TPropertyType).IsValueType) return;
                setter(ref typedInstance, value!);
            }
        }
    }
}