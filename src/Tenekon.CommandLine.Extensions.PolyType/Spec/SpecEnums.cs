namespace Tenekon.CommandLine.Extensions.PolyType.Spec;

/// <summary>
/// Flags that control which symbol names are auto-generated.
/// </summary>
[Flags]
public enum NameAutoGenerate
{
    /// <summary>No auto-generated names.</summary>
    None = 0,
    /// <summary>Auto-generate command names.</summary>
    Command = 1,
    /// <summary>Auto-generate option names.</summary>
    Option = 2,
    /// <summary>Auto-generate argument names.</summary>
    Argument = 4,
    /// <summary>Auto-generate directive names.</summary>
    Directive = 8,
    /// <summary>Auto-generate all supported symbol names.</summary>
    All = Command | Option | Argument | Directive
}

/// <summary>
/// Casing conventions used when formatting names.
/// </summary>
public enum NameCasingConvention
{
    /// <summary>No casing transformation is applied.</summary>
    None = 0,
    /// <summary>Lower-case transformation.</summary>
    LowerCase = 1,
    /// <summary>Upper-case transformation.</summary>
    UpperCase = 2,
    /// <summary>Title case transformation.</summary>
    TitleCase = 3,
    /// <summary>Pascal case transformation.</summary>
    PascalCase = 4,
    /// <summary>Camel case transformation.</summary>
    CamelCase = 5,
    /// <summary>Kebab case transformation.</summary>
    KebabCase = 6,
    /// <summary>Snake case transformation.</summary>
    SnakeCase = 7
}

/// <summary>
/// Prefix conventions used when formatting symbol names.
/// </summary>
public enum NamePrefixConvention
{
    /// <summary>No prefix is applied.</summary>
    None = 0,
    /// <summary>Prefix with a single hyphen.</summary>
    SingleHyphen = 1,
    /// <summary>Prefix with a double hyphen.</summary>
    DoubleHyphen = 2,
    /// <summary>Prefix with a forward slash.</summary>
    ForwardSlash = 3
}

/// <summary>
/// The allowed arity of an argument or option value.
/// </summary>
public enum ArgumentArity
{
    /// <summary>Accepts zero values.</summary>
    Zero = 0,
    /// <summary>Accepts zero or one value.</summary>
    ZeroOrOne = 1,
    /// <summary>Accepts exactly one value.</summary>
    ExactlyOne = 2,
    /// <summary>Accepts zero or more values.</summary>
    ZeroOrMore = 3,
    /// <summary>Accepts one or more values.</summary>
    OneOrMore = 4
}

/// <summary>
/// Built-in validation rules for option and argument values.
/// </summary>
[Flags]
public enum ValidationRules
{
    /// <summary>No built-in validation is applied.</summary>
    None = 0,
    /// <summary>Value must be an existing file.</summary>
    ExistingFile = 1,
    /// <summary>Value must be a non-existing file.</summary>
    NonExistingFile = 2,
    /// <summary>Value must be an existing directory.</summary>
    ExistingDirectory = 4,
    /// <summary>Value must be a non-existing directory.</summary>
    NonExistingDirectory = 8,
    /// <summary>Value must be an existing file or directory.</summary>
    ExistingFileOrDirectory = 16,
    /// <summary>Value must be a non-existing file or directory.</summary>
    NonExistingFileOrDirectory = 32,
    /// <summary>Value must be a legal path.</summary>
    LegalPath = 64,
    /// <summary>Value must be a legal file name.</summary>
    LegalFileName = 128,
    /// <summary>Value must be a legal absolute URI.</summary>
    LegalUri = 256,
    /// <summary>Value must be a legal HTTP/HTTPS URL.</summary>
    LegalUrl = 512
}
