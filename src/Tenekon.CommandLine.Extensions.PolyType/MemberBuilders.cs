using System.CommandLine;
using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType;

internal sealed record CommandMemberBuildResult(
    Symbol Symbol,
    Action<object, ParseResult> Binder,
    CommandDescriptor.SpecMember Member);

internal sealed class OptionMemberBuilder(
    IPropertyShape propertyShape,
    IPropertyShape valueProperty,
    OptionSpecAttribute spec,
    CommandLineNamer namer,
    object defaultInstance)
{
    public CommandMemberBuildResult? Build()
    {
        return (CommandMemberBuildResult?)propertyShape.Accept(new BuilderVisitor(spec, namer, defaultInstance, valueProperty));
    }

    private sealed class BuilderVisitor(
        OptionSpecAttribute spec,
        CommandLineNamer namer,
        object defaultInstance,
        IPropertyShape valueProperty)
        : TypeShapeVisitor
    {
        public override object? VisitProperty<TDeclaringType, TPropertyType>(
            IPropertyShape<TDeclaringType, TPropertyType> propertyShape,
            object? state = null)
        {
            var name = namer.GetOptionName(propertyShape.Name, spec.Name);
            var option = new Option<TPropertyType>(name)
            {
                Description = spec.Description ?? string.Empty,
                Hidden = spec.Hidden
            };

            if (spec.Recursive)
                option.Recursive = true;

            if (!string.IsNullOrWhiteSpace(spec.HelpName))
                option.HelpName = spec.HelpName;

            if (spec.IsAritySpecified)
                option.Arity = ArityHelper.Map(spec.Arity);

            if (spec.AllowMultipleArgumentsPerToken)
                option.AllowMultipleArgumentsPerToken = true;

            if (spec.AllowedValues is { Length: > 0 })
                option.AcceptOnlyFromAmong(spec.AllowedValues);

            ApplyAliases(option, spec, namer, name);

            var defaultValue = DefaultValueHelper.TryGetDefaultValue(
                valueProperty,
                defaultInstance,
                out var hasDefault);
            var required = RequiredHelper.IsRequired(valueProperty, spec, hasDefault, defaultValue);
            option.Required = required;

            if (hasDefault)
            {
                var value = (TPropertyType?)defaultValue;
                option.DefaultValueFactory = _ => value!;
            }

            ValidationHelper.Apply(option, spec.ValidationRules, spec.ValidationPattern, spec.ValidationMessage);

            var setter = propertyShape.GetSetter();
            Action<object, ParseResult> binder = (instance, parseResult) =>
            {
                var typedInstance = (TDeclaringType)instance;
                var value = parseResult.GetValue(option);
                if (value is null && !typeof(TPropertyType).IsValueType)
                    return;
                setter(ref typedInstance, value!);
            };

            var getter = PropertyAccessorFactory.CreateGetter(propertyShape);
            var member = new CommandDescriptor.SpecMember(propertyShape.Name, getter ?? (_ => null));

            return new CommandMemberBuildResult(option, binder, member);
        }

        private static void ApplyAliases<T>(
            Option<T> option,
            OptionSpecAttribute spec,
            CommandLineNamer namer,
            string baseName)
        {
            if (!string.IsNullOrWhiteSpace(spec.Alias))
            {
                var alias = namer.NormalizeOptionAlias(spec.Alias!, shortForm: false);
                namer.AddAlias(alias);
                option.Aliases.Add(alias);
            }

            if (spec.Aliases is not null)
                foreach (var alias in spec.Aliases)
                {
                    if (string.IsNullOrWhiteSpace(alias)) continue;
                    var normalized = namer.NormalizeOptionAlias(alias, shortForm: false);
                    namer.AddAlias(normalized);
                    option.Aliases.Add(normalized);
                }

            var shortForm = namer.CreateShortForm(baseName.TrimStart('-', '/'), forOption: true);
            if (!string.IsNullOrWhiteSpace(shortForm)) option.Aliases.Add(shortForm);
        }
    }
}

internal sealed class ArgumentMemberBuilder(
    IPropertyShape propertyShape,
    IPropertyShape valueProperty,
    ArgumentSpecAttribute spec,
    CommandLineNamer namer,
    object defaultInstance)
{
    public CommandMemberBuildResult? Build()
    {
        return (CommandMemberBuildResult?)propertyShape.Accept(new BuilderVisitor(spec, namer, defaultInstance, valueProperty));
    }

    private sealed class BuilderVisitor(
        ArgumentSpecAttribute spec,
        CommandLineNamer namer,
        object defaultInstance,
        IPropertyShape valueProperty)
        : TypeShapeVisitor
    {
        public override object? VisitProperty<TDeclaringType, TPropertyType>(
            IPropertyShape<TDeclaringType, TPropertyType> propertyShape,
            object? state = null)
        {
            var name = namer.GetArgumentName(propertyShape.Name, spec.Name);
            var argument = new Argument<TPropertyType>(name)
            {
                Description = spec.Description ?? string.Empty
            };

            argument.Hidden = spec.Hidden;

            if (!string.IsNullOrWhiteSpace(spec.HelpName))
                argument.HelpName = spec.HelpName;

            if (spec.IsAritySpecified)
                argument.Arity = ArityHelper.Map(spec.Arity);

            if (spec.AllowedValues is { Length: > 0 })
                ArgumentValidation.AcceptOnlyFromAmong(argument, spec.AllowedValues);

            var defaultValue = DefaultValueHelper.TryGetDefaultValue(
                valueProperty,
                defaultInstance,
                out var hasDefault);
            var required = RequiredHelper.IsRequired(valueProperty, spec, hasDefault, defaultValue);
            if (required && !spec.IsAritySpecified)
            {
                var propertyType = propertyShape.PropertyType.Type;
                if (propertyType == typeof(string))
                    argument.Arity = System.CommandLine.ArgumentArity.ExactlyOne;
                else if (propertyShape.PropertyType is IEnumerableTypeShape)
                    argument.Arity = System.CommandLine.ArgumentArity.OneOrMore;
                else
                    argument.Arity = System.CommandLine.ArgumentArity.ExactlyOne;
            }

            if (hasDefault)
            {
                var value = (TPropertyType?)defaultValue;
                argument.DefaultValueFactory = _ => value!;
            }

            ValidationHelper.Apply(argument, spec.ValidationRules, spec.ValidationPattern, spec.ValidationMessage);

            var setter = propertyShape.GetSetter();
            Action<object, ParseResult> binder = (instance, parseResult) =>
            {
                var typedInstance = (TDeclaringType)instance;
                var value = parseResult.GetValue(argument);
                setter(ref typedInstance, value!);
            };

            var getter = PropertyAccessorFactory.CreateGetter(propertyShape);
            var member = new CommandDescriptor.SpecMember(propertyShape.Name, getter ?? (_ => null));

            return new CommandMemberBuildResult(argument, binder, member);
        }
    }
}

internal sealed record DirectiveBuildResult(Directive Directive, Action<object, ParseResult> Binder);

internal sealed class DirectiveMemberBuilder(
    IPropertyShape propertyShape,
    DirectiveSpecAttribute spec,
    CommandLineNamer namer)
{
    public DirectiveBuildResult? Build()
    {
        return (DirectiveBuildResult?)propertyShape.Accept(new BuilderVisitor(spec, namer));
    }

    private sealed class BuilderVisitor(DirectiveSpecAttribute spec, CommandLineNamer namer) : TypeShapeVisitor
    {
        public override object? VisitProperty<TDeclaringType, TPropertyType>(
            IPropertyShape<TDeclaringType, TPropertyType> propertyShape,
            object? state = null)
        {
            var name = namer.GetDirectiveName(propertyShape.Name, spec.Name);
            var directive = new Directive(name);

            Action<object, ParseResult> binder = (instance, parseResult) =>
            {
                var result = parseResult.GetResult(directive) as System.CommandLine.Parsing.DirectiveResult;
                var typedInstance = (TDeclaringType)instance;
                var setter = propertyShape.GetSetter();

                object? value = null;
                if (typeof(TPropertyType) == typeof(bool))
                    value = result is not null;
                else if (typeof(TPropertyType) == typeof(string))
                    value = result?.Values is { Count: > 0 } ? result.Values[index: 0] : null;
                else if (typeof(TPropertyType) == typeof(string[]))
                    value = result?.Values?.ToArray() ?? [];
                else
                    return;

                setter(ref typedInstance, (TPropertyType)value!);
            };

            return new DirectiveBuildResult(directive, binder);
        }
    }
}

internal static class RequiredHelper
{
    public static bool IsRequired(
        IPropertyShape propertyShape,
        OptionSpecAttribute spec,
        bool hasDefault,
        object? defaultValue)
    {
        if (spec.IsRequiredSpecified) return spec.Required;
        return IsRequiredCore(propertyShape, hasDefault, defaultValue);
    }

    public static bool IsRequired(
        IPropertyShape propertyShape,
        ArgumentSpecAttribute spec,
        bool hasDefault,
        object? defaultValue)
    {
        if (spec.IsRequiredSpecified) return spec.Required;
        return IsRequiredCore(propertyShape, hasDefault, defaultValue);
    }

    private static bool IsRequiredCore(IPropertyShape propertyShape, bool hasDefault, object? defaultValue)
    {
        if (hasDefault) return false;
        if (propertyShape.PropertyType.Type.IsValueType) return false;
        if (defaultValue is not null) return false;
        return propertyShape.IsSetterNonNullable;
    }
}

internal static class DefaultValueHelper
{
    public static object? TryGetDefaultValue(IPropertyShape propertyShape, object defaultInstance, out bool hasDefault)
    {
        hasDefault = false;
        var getter = PropertyAccessorFactory.CreateGetter(propertyShape);
        if (getter is null) return null;

        var value = getter(defaultInstance);
        hasDefault = true;
        return value;
    }
}

internal static class ArityHelper
{
    public static System.CommandLine.ArgumentArity Map(ArgumentArity arity)
    {
        return arity switch
        {
            ArgumentArity.Zero => System.CommandLine.ArgumentArity.Zero,
            ArgumentArity.ZeroOrOne => System.CommandLine.ArgumentArity.ZeroOrOne,
            ArgumentArity.ExactlyOne => System.CommandLine.ArgumentArity.ExactlyOne,
            ArgumentArity.ZeroOrMore => System.CommandLine.ArgumentArity.ZeroOrMore,
            ArgumentArity.OneOrMore => System.CommandLine.ArgumentArity.OneOrMore,
            _ => System.CommandLine.ArgumentArity.ZeroOrMore
        };
    }
}
