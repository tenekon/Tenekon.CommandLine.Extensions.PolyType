using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Model;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Graph;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal sealed class ArgumentParameterBuilder(
    IParameterShape parameterShape,
    ArgumentSpecAttribute spec,
    CommandNamingPolicy namer,
    IFileSystem fileSystem)
{
    public ParameterBuildResult? Build()
    {
        return (ParameterBuildResult?)parameterShape.Accept(new BuilderVisitor(spec, namer, fileSystem));
    }

    private sealed class BuilderVisitor(ArgumentSpecAttribute spec, CommandNamingPolicy namer, IFileSystem fileSystem)
        : TypeShapeVisitor
    {
        public override object? VisitParameter<TArgumentState, TParameterType>(
            IParameterShape<TArgumentState, TParameterType> parameterShape,
            object? state = null)
        {
            var name = namer.GetArgumentName(parameterShape.Name, spec.Name);
            var required = RequiredHelper.IsRequired(parameterShape, spec);
            var argument = SymbolBuildHelper.CreateArgument<TParameterType>(
                name,
                spec,
                namer,
                required,
                parameterShape.ParameterType,
                fileSystem);

            var accessor = new RuntimeValueAccessor(
                parameterShape.Name,
                (_, parseResult) => parseResult.GetValue(argument));

            return new ParameterBuildResult(argument, accessor);
        }
    }
}