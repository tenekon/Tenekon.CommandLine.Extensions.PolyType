using System.Runtime.CompilerServices;
using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;

internal sealed class ConfigureCommandActionInvoker : TypeShapeVisitor
{
    public static ConfigureCommandActionInvoker Default { get; } = new();

    public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
    {
        return objectShape.Methods.SingleOrDefault(methodShape => methodShape is
            {
                Name: "ConfigureCommand",
                IsStatic: true,
                IsPublic: true,
                IsVoidLike: true,
                Parameters: [{ ParameterInfo.ParameterType: { } parameterType }]
            } && parameterType == typeof(ConfigureCommandContext))
            ?.Accept(this);
    }

    public override object? VisitMethod<TDeclaringType, TArgumentState, TResult>(
        IMethodShape<TDeclaringType, TArgumentState, TResult> methodShape,
        object? state = null)
    {
        var argumentStateCtor = methodShape.GetArgumentStateConstructor();
        var parameterSetter = methodShape.Parameters
            .Select(x => (Setter<TArgumentState, ConfigureCommandContext>)x.Accept(this, state: null)!)
            .Single();
        var methodInvoker = methodShape.GetMethodInvoker();

        return new Action<ConfigureCommandContext>(context =>
        {
            var argumentState = argumentStateCtor();
            parameterSetter(ref argumentState, context);
            methodInvoker.Invoke(ref Unsafe.NullRef<TDeclaringType>()!, ref argumentState);
        });
    }

    public override object? VisitParameter<TArgumentState, TParameterType>(
        IParameterShape<TArgumentState, TParameterType> parameterShape,
        object? state = null)
    {
        var setter = parameterShape.GetSetter();
        return new Setter<TArgumentState, ConfigureCommandContext>((ref argumentState, context) =>
        {
            setter(ref argumentState, (TParameterType)(object)context);
        });
    }

    private delegate void Setter<TArgumentState, TParameterType>(ref TArgumentState state, TParameterType value);
}