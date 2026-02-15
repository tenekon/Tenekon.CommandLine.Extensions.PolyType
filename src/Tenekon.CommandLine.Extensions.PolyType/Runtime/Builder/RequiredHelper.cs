using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal static class RequiredHelper
{
    public static bool IsRequired(IPropertyShape propertyShape, OptionSpecAttribute spec)
    {
        if (spec.IsRequiredSpecified) return spec.Required;
        return IsRequiredCore(propertyShape);
    }

    public static bool IsRequired(IPropertyShape propertyShape, ArgumentSpecAttribute spec)
    {
        if (spec.IsRequiredSpecified) return spec.Required;
        return IsRequiredCore(propertyShape);
    }

    public static bool IsRequired(IParameterShape parameterShape, OptionSpecAttribute spec)
    {
        if (spec.IsRequiredSpecified) return spec.Required;
        return IsRequiredCore(parameterShape);
    }

    public static bool IsRequired(IParameterShape parameterShape, ArgumentSpecAttribute spec)
    {
        if (spec.IsRequiredSpecified) return spec.Required;
        return IsRequiredCore(parameterShape);
    }

    private static bool IsRequiredCore(IPropertyShape propertyShape)
    {
        return propertyShape.IsSetterNonNullable;
    }

    private static bool IsRequiredCore(IParameterShape parameterShape)
    {
        return parameterShape.IsRequired || parameterShape.IsNonNullable;
    }
}