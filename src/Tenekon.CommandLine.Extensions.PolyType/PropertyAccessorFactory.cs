using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType;

internal static class PropertyAccessorFactory
{
    public static Func<object, object?>? CreateGetter(IPropertyShape propertyShape)
    {
        if (!propertyShape.HasGetter) return null;
        return (Func<object, object?>)propertyShape.Accept(new GetterVisitor())!;
    }

    public static Action<object, object>? CreateSetter(IPropertyShape propertyShape)
    {
        if (!propertyShape.HasSetter) return null;
        return (Action<object, object>)propertyShape.Accept(new SetterVisitor())!;
    }

    private sealed class GetterVisitor : TypeShapeVisitor
    {
        public override object? VisitProperty<TDeclaringType, TPropertyType>(
            IPropertyShape<TDeclaringType, TPropertyType> propertyShape,
            object? state = null)
        {
            var getter = propertyShape.GetGetter();
            return new Func<object, object?>(instance =>
            {
                var typed = (TDeclaringType)instance;
                return getter(ref typed);
            });
        }
    }

    private sealed class SetterVisitor : TypeShapeVisitor
    {
        public override object? VisitProperty<TDeclaringType, TPropertyType>(
            IPropertyShape<TDeclaringType, TPropertyType> propertyShape,
            object? state = null)
        {
            var setter = propertyShape.GetSetter();
            return new Action<object, object>((instance, value) =>
            {
                var typedInstance = (TDeclaringType)instance;
                var typedValue = value is null ? default! : (TPropertyType)value;
                setter(ref typedInstance, typedValue);
            });
        }
    }
}