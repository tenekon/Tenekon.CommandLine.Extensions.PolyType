using System.CommandLine;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Model;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal sealed class ArgumentMemberBuilder(
    IPropertyShape propertyShape,
    IPropertyShape valueProperty,
    ArgumentSpecAttribute spec,
    CommandNamingPolicy namer,
    IFileSystem fileSystem)
{
    public ArgumentMemberBuildResult? Build()
    {
        return (ArgumentMemberBuildResult?)propertyShape.Accept(
            new BuilderVisitor(spec, namer, valueProperty, fileSystem));
    }

    private sealed class BuilderVisitor(
        ArgumentSpecAttribute spec,
        CommandNamingPolicy namer,
        IPropertyShape valueProperty,
        IFileSystem fileSystem) : TypeShapeVisitor
    {
        public override object? VisitProperty<TDeclaringType, TPropertyType>(
            IPropertyShape<TDeclaringType, TPropertyType> propertyShape,
            object? state = null)
        {
            var name = namer.GetArgumentName(propertyShape.Name, spec.Name);
            var required = RequiredHelper.IsRequired(valueProperty, spec);
            var argument = SymbolBuildHelper.CreateArgument<TPropertyType>(
                name,
                spec,
                namer,
                required,
                propertyShape.PropertyType,
                fileSystem);

            var setter = propertyShape.GetSetter();
            Action<object, ParseResult> binder = (instance, parseResult) =>
            {
                var typedInstance = (TDeclaringType)instance;
                var value = parseResult.GetValue(argument);
                if (value is null && !typeof(TPropertyType).IsValueType) return;
                setter(ref typedInstance, value!);
            };

            return new ArgumentMemberBuildResult(argument, binder);
        }
    }
}