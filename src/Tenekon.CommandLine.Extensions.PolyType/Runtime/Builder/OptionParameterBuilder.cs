using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Model;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Graph;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal sealed class OptionParameterBuilder(
    IParameterShape parameterShape,
    OptionSpecAttribute spec,
    CommandNamingPolicy namer,
    IFileSystem fileSystem)
{
    public ParameterBuildResult? Build()
    {
        return (ParameterBuildResult?)parameterShape.Accept(new BuilderVisitor(spec, namer, fileSystem));
    }

    private sealed class BuilderVisitor(OptionSpecAttribute spec, CommandNamingPolicy namer, IFileSystem fileSystem)
        : TypeShapeVisitor
    {
        public override object? VisitParameter<TArgumentState, TParameterType>(
            IParameterShape<TArgumentState, TParameterType> parameterShape,
            object? state = null)
        {
            var name = namer.GetOptionName(parameterShape.Name, spec.Name);
            var required = RequiredHelper.IsRequired(parameterShape, spec);
            var option = SymbolBuildHelper.CreateOption<TParameterType>(name, spec, namer, required, fileSystem);

            var accessor = new RuntimeValueAccessor(
                parameterShape.Name,
                (_, parseResult) => parseResult.GetValue(option));

            return new ParameterBuildResult(option, accessor);
        }
    }
}