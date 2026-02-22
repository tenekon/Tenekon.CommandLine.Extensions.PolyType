using System.CommandLine;
using System.Runtime.InteropServices;
using PolyType;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Model;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Validation;
using ArgumentArity = System.CommandLine.ArgumentArity;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal static class SymbolBuildHelper
{
    public static Option<T> CreateOption<T>(
        string name,
        OptionSpecModel spec,
        CommandNamingPolicy namer,
        bool required,
        IFileSystem fileSystem)
    {
        var option = new Option<T>(name)
        {
            Description = spec.Description ?? string.Empty,
            Hidden = spec.Hidden
        };

        if (spec.Recursive) option.Recursive = true;

        if (!string.IsNullOrWhiteSpace(spec.HelpName)) option.HelpName = spec.HelpName;

        if (spec.IsAritySpecified) option.Arity = ArgumentArityHelper.Map(spec.Arity);

        if (spec.AllowMultipleArgumentsPerToken) option.AllowMultipleArgumentsPerToken = true;

        if (!spec.AllowedValues.IsDefaultOrEmpty) option.AcceptOnlyFromAmong(spec.AllowedValues.ToArray());

        ApplyAliases(option, spec, namer, name);

        option.Required = required;

        ValidationHelper.Apply(
            option,
            spec.ValidationRules,
            spec.ValidationPattern,
            spec.ValidationMessage,
            fileSystem);

        return option;
    }

    public static Argument<T> CreateArgument<T>(
        string name,
        ArgumentSpecModel spec,
        CommandNamingPolicy namer,
        bool required,
        ITypeShape valueType,
        IFileSystem fileSystem)
    {
        var argument = new Argument<T>(name)
        {
            Description = spec.Description ?? string.Empty,
            Hidden = spec.Hidden
        };

        if (!string.IsNullOrWhiteSpace(spec.HelpName)) argument.HelpName = spec.HelpName;

        if (spec.IsAritySpecified) argument.Arity = ArgumentArityHelper.Map(spec.Arity);

        if (!spec.AllowedValues.IsDefaultOrEmpty)
            argument.AcceptOnlyFromAmong(ImmutableCollectionsMarshal.AsArray(spec.AllowedValues) ?? []);

        if (required && !spec.IsAritySpecified)
        {
            if (valueType.Type == typeof(string))
                argument.Arity = ArgumentArity.ExactlyOne;
            else if (valueType is IEnumerableTypeShape)
                argument.Arity = ArgumentArity.OneOrMore;
            else
                argument.Arity = ArgumentArity.ExactlyOne;
        }

        ValidationHelper.Apply(
            argument,
            spec.ValidationRules,
            spec.ValidationPattern,
            spec.ValidationMessage,
            fileSystem);

        return argument;
    }

    private static void ApplyAliases<T>(
        Option<T> option,
        OptionSpecModel spec,
        CommandNamingPolicy namer,
        string baseName)
    {
        if (!string.IsNullOrWhiteSpace(spec.Alias))
        {
            var alias = namer.NormalizeOptionAlias(spec.Alias!, shortForm: false);
            namer.AddAlias(alias);
            option.Aliases.Add(alias);
        }

        if (!spec.Aliases.IsDefaultOrEmpty)
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
