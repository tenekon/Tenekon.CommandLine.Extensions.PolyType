using System.Text;
using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal static class CommandModelValidation
{
    public static void EnsureFunctionTypeValid(Type type)
    {
        if (type.IsGenericType || type.ContainsGenericParameters || IsGenericDeclaringType(type))
            throw new InvalidOperationException($"Function '{type.FullName}' cannot be generic.");
    }

    public static void EnsureMethodOverloadsValid(
        Type declaringType,
        IReadOnlyList<(IMethodShape Method, CommandSpecModel Spec)> methods)
    {
        var byName = methods.GroupBy(entry => entry.Method.Name, StringComparer.Ordinal);
        foreach (var group in byName)
        {
            if (group.Count() > 1)
                foreach (var entry in group)
                    if (string.IsNullOrWhiteSpace(entry.Spec.Name))
                        throw new InvalidOperationException(
                            $"Command method '{declaringType.FullName}.{entry.Method.Name}' requires an explicit name.");

            var signatures = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in group)
            {
                var signature = CreateMethodSignature(entry.Method);
                if (!signatures.Add(signature))
                    throw new InvalidOperationException(
                        $"Command method '{declaringType.FullName}.{entry.Method.Name}' has a duplicate signature.");
            }
        }
    }

    public static void EnsureMethodNodeValid(Type declaringType, IMethodShape method, CommandSpecModel spec)
    {
        if (method.IsStatic)
            throw new InvalidOperationException(
                $"Command method '{declaringType.FullName}.{method.Name}' cannot be static.");

        if (method.DeclaringType.Type.IsInterface)
            throw new InvalidOperationException(
                $"Command method '{declaringType.FullName}.{method.Name}' cannot be declared on an interface.");

        if (method.MethodBase?.IsGenericMethod == true)
            throw new InvalidOperationException(
                $"Command method '{declaringType.FullName}.{method.Name}' cannot be generic.");

        if (IsGenericDeclaringType(method.DeclaringType.Type))
            throw new InvalidOperationException(
                $"Command method '{declaringType.FullName}.{method.Name}' cannot be declared on a generic type.");

        if (spec.Parent is not null && spec.Parent != declaringType)
            throw new InvalidOperationException(
                $"Command method '{declaringType.FullName}.{method.Name}' can only set Parent to its declaring type.");
    }

    public static bool IsGenericDeclaringType(Type type)
    {
        var current = type;
        while (current is not null)
        {
            if (current.IsGenericType || current.ContainsGenericParameters) return true;
            current = current.DeclaringType;
        }

        return false;
    }

    private static string CreateMethodSignature(IMethodShape method)
    {
        var builder = new StringBuilder();
        builder.Append(method.Name);
        builder.Append(value: '(');
        var first = true;
        foreach (var parameter in method.Parameters)
        {
            if (!first) builder.Append(value: ',');
            builder.Append(parameter.ParameterType.Type.FullName ?? parameter.ParameterType.Type.Name);
            first = false;
        }

        builder.Append(value: ')');
        return builder.ToString();
    }
}