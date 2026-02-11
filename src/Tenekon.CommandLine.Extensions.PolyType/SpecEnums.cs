namespace Tenekon.CommandLine.Extensions.PolyType;

[Flags]
public enum NameAutoGenerate
{
    None = 0,
    Command = 1,
    Option = 2,
    Argument = 4,
    Directive = 8,
    All = Command | Option | Argument | Directive
}

public enum NameCasingConvention
{
    None = 0,
    LowerCase = 1,
    UpperCase = 2,
    TitleCase = 3,
    PascalCase = 4,
    CamelCase = 5,
    KebabCase = 6,
    SnakeCase = 7
}

public enum NamePrefixConvention
{
    None = 0,
    SingleHyphen = 1,
    DoubleHyphen = 2,
    ForwardSlash = 3
}

public enum ArgumentArity
{
    Zero = 0,
    ZeroOrOne = 1,
    ExactlyOne = 2,
    ZeroOrMore = 3,
    OneOrMore = 4
}

[Flags]
public enum ValidationRules
{
    None = 0,
    ExistingFile = 1,
    NonExistingFile = 2,
    ExistingDirectory = 4,
    NonExistingDirectory = 8,
    ExistingFileOrDirectory = 16,
    NonExistingFileOrDirectory = 32,
    LegalPath = 64,
    LegalFileName = 128,
    LegalUri = 256,
    LegalUrl = 512
}