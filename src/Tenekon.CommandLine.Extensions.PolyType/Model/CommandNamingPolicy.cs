using System.Text;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal sealed class CommandNamingPolicy(
    NameAutoGenerate? nameAutoGenerate,
    NameCasingConvention? nameCasingConvention,
    NamePrefixConvention? namePrefixConvention,
    NameAutoGenerate? shortFormAutoGenerate,
    NamePrefixConvention? shortFormPrefixConvention,
    CommandNamingPolicy? parent = null)
{
    private readonly CommandNamingPolicy? _parent = parent;

    private static readonly string[] CommandSuffixes =
    [
        "RootCliCommand", "RootCommand", "SubCliCommand", "SubCommand", "CliCommand", "Command", "Cli"
    ];

    private static readonly string[] OptionSuffixes =
    [
        "Option",
        "RootCliCommandOption", "RootCommandOption", "SubCliCommandOption", "SubCommandOption",
        "CliCommandOption", "CommandOption", "CliOption"
    ];

    private static readonly string[] ArgumentSuffixes =
    [
        "Argument",
        "RootCliCommandArgument", "RootCommandArgument", "SubCliCommandArgument", "SubCommandArgument",
        "CliCommandArgument", "CommandArgument", "CliArgument"
    ];

    private static readonly string[] DirectiveSuffixes =
    [
        "Directive",
        "RootCliCommandDirective", "RootCommandDirective", "SubCliCommandDirective", "SubCommandDirective",
        "CliCommandDirective", "CommandDirective", "CliDirective"
    ];

    private readonly HashSet<string> _commandNames = new(StringComparer.Ordinal);

    private readonly HashSet<string> _optionNames = new(StringComparer.Ordinal);

    private readonly HashSet<string> _argumentNames = new(StringComparer.Ordinal);

    private readonly HashSet<string> _directiveNames = parent?._directiveNames
        ?? new HashSet<string>(StringComparer.Ordinal);

    private readonly HashSet<string> _aliases = new(StringComparer.Ordinal);

    private readonly NameAutoGenerate _nameAutoGenerate = nameAutoGenerate
        ?? parent?._nameAutoGenerate ?? NameAutoGenerate.All;

    private readonly NameCasingConvention _nameCasingConvention = nameCasingConvention
        ?? parent?._nameCasingConvention ?? NameCasingConvention.KebabCase;

    private readonly NamePrefixConvention _namePrefixConvention = namePrefixConvention
        ?? parent?._namePrefixConvention ?? NamePrefixConvention.DoubleHyphen;

    private readonly NameAutoGenerate _shortFormAutoGenerate = shortFormAutoGenerate
        ?? parent?._shortFormAutoGenerate ?? NameAutoGenerate.All;

    private readonly NamePrefixConvention _shortFormPrefixConvention = shortFormPrefixConvention
        ?? parent?._shortFormPrefixConvention ?? NamePrefixConvention.SingleHyphen;

    public string GetCommandName(string fallback, string? explicitName = null)
    {
        var name = ResolveName(
            explicitName,
            fallback,
            NameAutoGenerate.Command,
            CommandSuffixes,
            _nameCasingConvention);
        name = ApplyCasing(name, _nameCasingConvention);
        var set = _parent?._commandNames ?? _commandNames;
        EnsureUnique(set, name, "command");
        return name;
    }

    public string GetOptionName(string fallback, string? explicitName = null)
    {
        var name = ResolveName(explicitName, fallback, NameAutoGenerate.Option, OptionSuffixes, _nameCasingConvention);
        name = ApplyCasing(name, _nameCasingConvention);
        name = ApplyPrefix(name, _namePrefixConvention);
        EnsureUnique(_optionNames, name, "option");
        return name;
    }

    public string GetArgumentName(string fallback, string? explicitName = null)
    {
        var name = ResolveName(
            explicitName,
            fallback,
            NameAutoGenerate.Argument,
            ArgumentSuffixes,
            _nameCasingConvention);
        name = ApplyCasing(name, _nameCasingConvention);
        EnsureUnique(_argumentNames, name, "argument");
        return name;
    }

    public string GetDirectiveName(string fallback, string? explicitName = null)
    {
        var name = ResolveName(
            explicitName,
            fallback,
            NameAutoGenerate.Directive,
            DirectiveSuffixes,
            _nameCasingConvention);
        name = ApplyCasing(name, _nameCasingConvention);
        EnsureUnique(_directiveNames, name, "directive");
        return name;
    }

    public void AddAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) return;
        if (!_aliases.Add(alias))
            throw new InvalidOperationException($"Alias '{alias}' is already used in this command hierarchy.");
    }

    public string NormalizeOptionAlias(string alias, bool shortForm)
    {
        return ApplyPrefix(alias, shortForm ? _shortFormPrefixConvention : _namePrefixConvention);
    }

    public string? CreateShortForm(string baseName, bool forOption)
    {
        if (!ShouldAutoGenerateShortForm(forOption)) return null;

        var shortForm = BuildShortForm(baseName);
        if (string.IsNullOrWhiteSpace(shortForm)) return null;

        shortForm = ApplyCasing(shortForm, _nameCasingConvention);
        if (forOption) shortForm = ApplyPrefix(shortForm, _shortFormPrefixConvention);

        if (_aliases.Contains(shortForm)) return null;
        _aliases.Add(shortForm);
        return shortForm;
    }

    private bool ShouldAutoGenerateShortForm(bool forOption)
    {
        return forOption
            ? _shortFormAutoGenerate.HasFlag(NameAutoGenerate.Option)
            : _shortFormAutoGenerate.HasFlag(NameAutoGenerate.Command);
    }

    private string ResolveName(
        string? explicitName,
        string fallback,
        NameAutoGenerate flag,
        IReadOnlyList<string> suffixes,
        NameCasingConvention casing)
    {
        if (!string.IsNullOrWhiteSpace(explicitName)) return explicitName;

        if (!_nameAutoGenerate.HasFlag(flag)) return fallback;

        var stripped = StripSuffixes(fallback, suffixes);
        return ApplyCasing(stripped, casing);
    }

    private static string StripSuffixes(string name, IReadOnlyList<string> suffixes)
    {
        foreach (var suffix in suffixes)
            if (name.EndsWith(suffix, StringComparison.Ordinal))
                return name.Substring(startIndex: 0, name.Length - suffix.Length);

        return name;
    }

    private static void EnsureUnique(HashSet<string> set, string name, string symbolKind)
    {
        if (!set.Add(name))
            throw new InvalidOperationException(
                $"The {symbolKind} name '{name}' is already used in this command hierarchy.");
    }

    private static string ApplyPrefix(string name, NamePrefixConvention prefixConvention)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        if (name.StartsWith("-", StringComparison.Ordinal)
            || name.StartsWith("/", StringComparison.Ordinal)) return name;

        return prefixConvention switch
        {
            NamePrefixConvention.SingleHyphen => "-" + name,
            NamePrefixConvention.DoubleHyphen => "--" + name,
            NamePrefixConvention.ForwardSlash => "/" + name,
            _ => name
        };
    }

    private static string ApplyCasing(string name, NameCasingConvention casing)
    {
        if (string.IsNullOrEmpty(name)) return name;

        return casing switch
        {
            NameCasingConvention.None => name,
            NameCasingConvention.LowerCase => name.ToLowerInvariant(),
            NameCasingConvention.UpperCase => name.ToUpperInvariant(),
            NameCasingConvention.TitleCase => ToTitleCase(name),
            NameCasingConvention.PascalCase => ToPascalCase(name),
            NameCasingConvention.CamelCase => ToCamelCase(name),
            NameCasingConvention.KebabCase => ToKebabCase(name),
            NameCasingConvention.SnakeCase => ToSnakeCase(name),
            _ => name
        };
    }

    private static string ToTitleCase(string name)
    {
        return JoinWords(
            name,
            word => char.ToUpperInvariant(word[index: 0]) + word.Substring(startIndex: 1).ToLowerInvariant());
    }

    private static string ToPascalCase(string name)
    {
        return JoinWords(name, word => char.ToUpperInvariant(word[index: 0]) + word.Substring(startIndex: 1));
    }

    private static string ToCamelCase(string name)
    {
        var pascal = ToPascalCase(name);
        if (pascal.Length == 0) return pascal;
        return char.ToLowerInvariant(pascal[index: 0]) + pascal.Substring(startIndex: 1);
    }

    private static string ToKebabCase(string name)
    {
        var words = SplitWords(name);
        return string.Join("-", words.Where(w => w.Length > 0).Select(w => w.ToLowerInvariant()));
    }

    private static string ToSnakeCase(string name)
    {
        var words = SplitWords(name);
        return string.Join("_", words.Where(w => w.Length > 0).Select(w => w.ToLowerInvariant()));
    }

    private static string JoinWords(string name, Func<string, string> transform)
    {
        var words = SplitWords(name);
        if (words.Count == 0) return string.Empty;

        var builder = new StringBuilder();
        foreach (var word in words)
        {
            if (word.Length == 0) continue;
            builder.Append(transform(word));
        }

        return builder.ToString();
    }

    private static string BuildShortForm(string name)
    {
        var words = SplitWords(name);
        var builder = new StringBuilder();
        foreach (var word in words)
        {
            if (word.Length == 0) continue;

            if (word.All(char.IsDigit))
                builder.Append(word);
            else
                builder.Append(word[index: 0]);
        }

        return builder.ToString();
    }

    private static IReadOnlyList<string> SplitWords(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return [];

        var normalized = name.Replace("-", " ").Replace("_", " ");
        var parts = new List<string>();
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length > 0)
            {
                parts.Add(current.ToString());
                current.Clear();
            }
        }

        for (var i = 0; i < normalized.Length; i++)
        {
            var ch = normalized[i];
            if (char.IsWhiteSpace(ch))
            {
                Flush();
                continue;
            }

            if (current.Length > 0)
            {
                var prev = current[current.Length - 1];
                if (char.IsDigit(ch) && !char.IsDigit(prev))
                    Flush();
                else if (!char.IsDigit(ch) && char.IsDigit(prev))
                    Flush();
                else if (char.IsUpper(ch) && !char.IsUpper(prev)) Flush();
            }

            current.Append(ch);
        }

        Flush();
        return parts;
    }
}