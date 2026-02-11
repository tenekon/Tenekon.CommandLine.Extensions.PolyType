using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType;

internal sealed class ConstructorFactory : TypeShapeVisitor
{
    public override object? VisitConstructor<TDeclaringType, TArgumentState>(
        IConstructorShape<TDeclaringType, TArgumentState> constructorShape,
        object? state = null)
    {
        if (constructorShape.Parameters.Count == 0)
        {
            var defaultCtor = constructorShape.GetDefaultConstructor();
            return new Func<IServiceProvider?, object>(_ => defaultCtor()!);
        }

        var argumentStateCtor = constructorShape.GetArgumentStateConstructor();
        var ctor = constructorShape.GetParameterizedConstructor();

        var parameterSetters = constructorShape.Parameters
            .Select(parameter => (ArgumentStateSetter<TArgumentState>)parameter.Accept(new ParameterSetterVisitor())!)
            .ToArray();

        return new Func<IServiceProvider?, object>(serviceProvider =>
        {
            var argumentState = argumentStateCtor();
            foreach (var setter in parameterSetters)
                setter(ref argumentState, serviceProvider);

            var instance = ctor(ref argumentState);
            return instance!;
        });
    }

    private delegate void ArgumentStateSetter<TArgumentState>(ref TArgumentState state, IServiceProvider? provider);

    private sealed class ParameterSetterVisitor : TypeShapeVisitor
    {
        public override object? VisitParameter<TArgumentState, TParameterType>(
            IParameterShape<TArgumentState, TParameterType> parameterShape,
            object? state = null)
        {
            var setter = parameterShape.GetSetter();

            return new ArgumentStateSetter<TArgumentState>((
                ref TArgumentState argumentState,
                IServiceProvider? provider) =>
            {
                var resolved = provider?.GetService(typeof(TParameterType));
                TParameterType value;

                if (resolved is TParameterType typed)
                {
                    value = typed;
                }
                else if (resolved is null)
                {
                    if (parameterShape.HasDefaultValue)
                        value = parameterShape.DefaultValue!;
                    else if (!parameterShape.IsRequired)
                        value = default!;
                    else
                        throw new InvalidOperationException(
                            $"Unable to resolve required constructor parameter '{parameterShape.Name}'.");
                }
                else
                {
                    value = (TParameterType)resolved;
                }

                setter(ref argumentState, value);
            });
        }
    }
}
