using System.CommandLine;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Model;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal sealed class OptionMemberBuilder(
    IPropertyShape specProperty,
    IPropertyShape targetProperty,
    OptionSpecModel spec,
    CommandNamingPolicy namer,
    IFileSystem fileSystem)
{
    public ArgumentMemberBuildResult? Build()
    {
        return (ArgumentMemberBuildResult?)specProperty.Accept(
            new BuilderVisitor(spec, namer, targetProperty, fileSystem));
    }

    private sealed class BuilderVisitor(
        OptionSpecModel spec,
        CommandNamingPolicy namer,
        IPropertyShape targetProperty,
        IFileSystem fileSystem) : TypeShapeVisitor
    {
        public override object? VisitProperty<TDeclaringType, TPropertyType>(
            IPropertyShape<TDeclaringType, TPropertyType> propertyShape,
            object? state = null)
        {
            var name = namer.GetOptionName(propertyShape.Name, spec.Name);
            var required = RequiredHelper.IsRequired(targetProperty, spec);
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
