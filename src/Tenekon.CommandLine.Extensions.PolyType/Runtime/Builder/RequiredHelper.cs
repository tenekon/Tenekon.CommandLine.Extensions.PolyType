using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Model;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal static class RequiredHelper
{
    public static bool IsRequired(IPropertyShape propertyShape, OptionSpecModel spec)
    {
        if (spec.IsRequiredSpecified) return spec.Required;
        return IsRequiredCore(propertyShape);
    }

    public static bool IsRequired(IPropertyShape propertyShape, ArgumentSpecModel spec)
    {
        if (spec.IsRequiredSpecified) return spec.Required;
        return IsRequiredCore(propertyShape);
    }

    public static bool IsRequired(IParameterShape parameterShape, OptionSpecModel spec)
    {
        if (spec.IsRequiredSpecified) return spec.Required;
        return IsRequiredCore(parameterShape);
    }

    public static bool IsRequired(IParameterShape parameterShape, ArgumentSpecModel spec)
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